using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.FileTransport
{
    public sealed class FileBusConnection : IMessageSink, IMessageSource, IDisposable
    {
        private SubscriptionDescription subscription;
        private readonly string endpointName;
        private readonly ITypeMap typeMap;
        private readonly FileOperations fileOperations;

        public FileBusConnection(string busPath, string endpointName, ITypeMap typeMap)
        {
            this.endpointName = endpointName;
            this.typeMap = typeMap;
            fileOperations = new FileOperations(busPath, endpointName);
        }

        public void Dispose()
        {
            fileOperations.DeleteAllSubscriberFiles();
        }

        public Task Close() => Task.CompletedTask;

        public TimeSpan LockTime { get; set; } = TimeSpan.FromMinutes(1);

        public Task Abandon(IncomingMessage message)
        {
            var file = fileOperations.GetHiddenFileForMessage(message);
            fileOperations.MoveOutOfHiddenFolder(file);
            return Task.CompletedTask;
        }

        public Task Complete(IncomingMessage message)
        {
            var file = fileOperations.GetHiddenFileForMessage(message);
            fileOperations.Delete(file);
            return Task.CompletedTask;
        }

        // TODO Unit test
        public Task DeadLetter(IncomingMessage message, string deadLetterReason, string deadLetterReasonDetail)
        {
            var file = fileOperations.GetHiddenFileForMessage(message);
            fileOperations.MoveToDeadLetterFolder(file);
            return Task.CompletedTask;
        }

        public Task EnsureSubscribed(SubscriptionDescription subscription, CancellationToken cancellationToken)
        {
            // TODO Unit test
            if (this.subscription != null)
            {
                throw new InvalidOperationException("Subscription already registered.");
            }

            if (subscription.EndpointName != endpointName)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(subscription),
                    "Endpoint name in subscription description does not match endpoint name of bus connection.");
            }

            this.subscription = subscription;
            return Task.CompletedTask;
        }

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
        public Task Sink(IEnumerable<OutgoingMessage> messages)
        {
            // TODO Inject serialiser

            foreach (var message in messages)
            {
                var text = JsonConvert.SerializeObject(message);

                foreach (var subscriberPath in fileOperations.GetAllSubscriberPaths())
                {
                    var fileName = FileName.Create(subscriberPath, message.Id);
                    fileName.WriteAllUtf8Text(text);
                }
            }

            // TODO Async file io library?
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyCollection<IncomingMessage>> WaitForNextMessageBatch(int maximumMessagesToReturn, CancellationToken cancellationToken)
        {
            if (subscription == null)
            {
                throw new InvalidOperationException();
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var batch = new List<IncomingMessage>();
                var utcNow = DateTime.UtcNow;
                foreach (var file in fileOperations.GetHiddenFiles())
                {
                    if (file.HasTimestampPassed(utcNow))
                    {
                        fileOperations.MoveOutOfHiddenFolder(file);
                    }
                }

                foreach (var file in fileOperations.GetQueuedFiles())
                {
                    var jObject = file.ReadContentsAsJObject();
                    var messageTypeNames = jObject["MessageTypeNames"]
                        .Select(token => token.Value<string>())
                        .ToArray();

                    if (!IsInterestedInMessage(
                        messageTypeNames.First(),
                        jObject["SpecificReceivingEndpointName"].Value<string>()))
                    {
                        fileOperations.Delete(file);
                        continue;
                    }

                    var type = typeMap.GetTypeByName(messageTypeNames.First());
                    var body = jObject["Body"].ToObject(type);
                    var message = new IncomingMessage(
                        jObject["Id"].Value<string>(),
                        body,
                        messageTypeNames,
                        DateTime.UtcNow,
                        DateTime.UtcNow + LockTime,
                        file.DequeueCount + 1,
                        jObject["Headers"].ToObject<HeaderCollection>());
                    batch.Add(message);

                    fileOperations.DequeueAndMoveToHiddenFolder(file, DateTime.UtcNow + LockTime);

                    if (batch.Count >= maximumMessagesToReturn)
                    {
                        break;
                    }
                }

                if (batch.Count > 0)
                {
                    return batch.AsReadOnly();
                }
                else
                {
                    await Task
                        .Delay(TimeSpan.FromSeconds(2))
                        .ConfigureAwait(false);
                }
            }

            return new List<IncomingMessage>().AsReadOnly();
        }

        private bool IsInterestedInMessage(string bodyTypeName, string specificReceivingEndpointName)
        {
            if (!string.IsNullOrEmpty(specificReceivingEndpointName))
            {
                return specificReceivingEndpointName == subscription.EndpointName;
            }

            return subscription.MessageTypeNames.Contains(bodyTypeName);
        }

        public Task DeferUntil(IncomingMessage message, DateTime scheduledTimeUtc, string deferralReason, string deferralErrorDescription)
        {
            var file = fileOperations.GetHiddenFileForMessage(message);
            fileOperations.RefreshHiddenUntil(file, scheduledTimeUtc);
            return Task.CompletedTask;
        }
    }
}
