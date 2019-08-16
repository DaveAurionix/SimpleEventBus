using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;

namespace SimpleEventBus.InMemoryTransport
{
    sealed class InMemoryBusConnection : IMessageSink, IMessageSource, IDisposable
    {
        private static readonly IReadOnlyCollection<IncomingMessage> emptyCollection = new List<IncomingMessage>().AsReadOnly();
        private readonly InMemoryBus bus;
        private SubscriptionDescription subscription;
        private readonly ConcurrentQueue<QueuedMessage> localQueue = new ConcurrentQueue<QueuedMessage>();
        private readonly HiddenMessages temporarilyHiddenMessages = new HiddenMessages();

        public InMemoryBusConnection(InMemoryBus bus)
        {
            this.bus = bus;
            bus.Connect(OnMessageReceive);
        }

        public void Dispose()
        {
            bus.Disconnect(OnMessageReceive);
        }

        public Task Close() => Task.CompletedTask;

        public Task Sink(IEnumerable<OutgoingMessage> messages)
        {
            bus.Publish(messages);
            return Task.CompletedTask;
        }

        public Task EnsureSubscribed(SubscriptionDescription subscription, CancellationToken cancellationToken)
        {
            // TODO Unit test
            if (this.subscription != null)
            {
                throw new InvalidOperationException("Subscription already registered.");
            }

            this.subscription = subscription;
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyCollection<IncomingMessage>> WaitForNextMessageBatch(int maximumMessagesToReturn, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    temporarilyHiddenMessages.MoveAnyDueHiddenMessagesTo(localQueue);

                    if (localQueue.IsEmpty)
                    {
                        await Task
                            .Delay(TimeSpan.FromSeconds(3), cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    var results = new List<IncomingMessage>();
                    while (results.Count < maximumMessagesToReturn
                        && localQueue.TryDequeue(out var message))
                    {
                        var returnedMessage = message.DequeueToIncoming(LockTime);
                        results.Add(returnedMessage);
                        temporarilyHiddenMessages.Add(returnedMessage.LockExpiresUtc, message);
                    }

                    return results.AsReadOnly();
                }
            }
            catch (TaskCanceledException)
            {
            }

            return emptyCollection;
        }

        public TimeSpan LockTime { get; set; } = TimeSpan.FromMinutes(1);

        public Task Abandon(IncomingMessage message)
        {
            // Azure Service Bus and MSMQ both restore the message at the top of the queue. This in-memory bus does not.
            localQueue.Enqueue(temporarilyHiddenMessages.Remove(message.Id));
            return Task.CompletedTask;
        }

        public Task Complete(IncomingMessage message)
        {
            temporarilyHiddenMessages.Remove(message.Id);
            return Task.CompletedTask;
        }

        private void OnMessageReceive(IEnumerable<OutgoingMessage> messages)
        {
            if (subscription == null)
            {
                return;
            }

            foreach (var message in messages)
            {
                if (message.SpecificReceivingEndpointName != null)
                {
                    if (string.Equals(message.SpecificReceivingEndpointName, subscription.EndpointName, StringComparison.OrdinalIgnoreCase))
                    {
                        localQueue.Enqueue(QueuedMessage.FromOutgoing(message));
                    }

                    continue;
                }

                foreach (var subscribedToMappedTypeName in subscription.MessageTypeNames)
                {
                    if (string.Equals(message.MessageTypeNames.First(), subscribedToMappedTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        localQueue.Enqueue(QueuedMessage.FromOutgoing(message));
                    }
                }
            }
        }

        public Task DeadLetter(IncomingMessage message, string deadLetterReason, string deadLetterReasonDetail)
        {
            // The in-memory bus just deletes deadletters
            temporarilyHiddenMessages.Remove(message.Id);
            return Task.CompletedTask;
        }

        public Task DeferUntil(IncomingMessage message, DateTime scheduledTimeUtc, string deferralReason, string deferralErrorDescription)
        {
            temporarilyHiddenMessages.RefreshBecomesVisibleAt(message.Id, scheduledTimeUtc);
            return Task.CompletedTask;
        }
    }
}
