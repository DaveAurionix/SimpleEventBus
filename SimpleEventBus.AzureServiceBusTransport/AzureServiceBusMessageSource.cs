using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleSubscriptionDescription = SimpleEventBus.Abstractions.Incoming.SubscriptionDescription;

namespace SimpleEventBus.AzureServiceBusTransport
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
    class AzureServiceBusMessageSource : IMessageSource
    {
        readonly AzureServiceBusTransportSettings settings;
        readonly ISubscriptionInitialiser subscriptionInitialiser;
        readonly IAzureServiceBusInstance[] busInstances;
        readonly ITypeMap typeMap;
        readonly ILogger<AzureServiceBusMessageSource> logger;
        readonly string thisEndpointName;

        public AzureServiceBusMessageSource(
            AzureServiceBusTransportSettings settings,
            ISubscriptionInitialiser subscriptionInitialiser,
            IEnumerable<IAzureServiceBusInstance> busInstances,
            ITypeMap typeMap,
            ILogger<AzureServiceBusMessageSource> logger,
            string endpointName)
        {
            this.busInstances = busInstances.ToArray();
            this.settings = settings;
            this.subscriptionInitialiser = subscriptionInitialiser;
            this.typeMap = typeMap;
            this.logger = logger;
            thisEndpointName = endpointName;
        }

        public Task Close()
            => Task.WhenAll(
                busInstances.Select(
                    busInstance => busInstance.TryShutDown()));

        public Task Abandon(IncomingMessage message)
            => ((ProviderData)message.ProviderData).Abandon();

        public Task Complete(IncomingMessage message)
            => ((ProviderData)message.ProviderData).Complete();

        public Task DeadLetter(IncomingMessage message, string deadLetterReason, string deadLetterReasonDetail)
            => ((ProviderData)message.ProviderData).DeadLetter(deadLetterReason, deadLetterReasonDetail);

        public Task DeferUntil(IncomingMessage message, DateTime scheduledTimeUtc, string deferralReason, string deferralReasonDetail)
            => ((ProviderData)message.ProviderData)
                .DeferOnSameBusInstanceUntil(scheduledTimeUtc, deferralReason, deferralReasonDetail, thisEndpointName);

        public async Task EnsureSubscribed(
            SimpleSubscriptionDescription subscription, CancellationToken cancellationToken)
        {
            var exceptions = new List<ServiceBusException>();
            var connectionStringNumber = 0;

            foreach (var connectionString in settings.ConnectionStrings)
            {
                connectionStringNumber++;

                try
                {
                    await subscriptionInitialiser
                        .EnsureInitialised(subscription, connectionString, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (ServiceBusException exception) when (exception.IsTransient)
                {
                    exceptions.Add(exception);

                    logger.LogWarning(
                        exception,
                        $"Transient exception encountered when initialising subscription using connection {connectionStringNumber}.  Assuming subscription rules are up to date for the Service Bus Topic on that connection and continuing anyway.  Exception message was \"{exception.Message}\"");
                }
            }

            if (exceptions.Count == settings.ConnectionStrings.Count())
            {
                throw new AggregateException("No Service Bus connections could be initialised.", exceptions);
            }
        }

        public async Task<IReadOnlyCollection<IncomingMessage>> WaitForNextMessageBatch(int maximumMessagesToReturn, CancellationToken cancellationToken)
        {
            var waitIfQueueEmpty = true;
            var trippedBreakers = 0;
            foreach (var busInstance in busInstances)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (busInstance.IsCircuitBreakerTripped)
                {
                    trippedBreakers++;
                    continue;
                }

                var azureMessages = await busInstance
                    .TryReceiveMessages(maximumMessagesToReturn, waitIfQueueEmpty)
                    .ConfigureAwait(false);

                if (azureMessages.Count != 0)
                {
                    IList<Message> deferredMessages = null;

                    if (azureMessages.Any(message => message.IsDeferredMarkerMessage()))
                    {
                        deferredMessages = await FetchDeferredMessages(azureMessages, busInstance)
                            .ConfigureAwait(false);
                    }

                    return azureMessages
                        .Select(
                            azureMessage => BuildMessage(
                                azureMessage,
                                deferredMessages,
                                busInstance))
                        .ToList()
                        .AsReadOnly();
                }

                // There are no messages on the first tested connection but it was healthy.  Perform a fast check of remaining connections just to make sure that no messages are queued up
                waitIfQueueEmpty = false;
            }

            if (trippedBreakers == busInstances.Length)
            {
                // All receivers have tripped circuit-breakers, delay before trying again to avoid high CPU spikes and lots of attempted network traffic.
                await Task
                    .Delay(settings.BackoffDelayIfAllConnectionsFaulty)
                    .ConfigureAwait(false);
            }

            return IncomingMessage.EmptyReadOnlyCollection;
        }

        private async Task<IList<Message>> FetchDeferredMessages(IList<Message> originalMessages, IAzureServiceBusInstance busInstance)
        {
            var sequenceNumbers = from originalMessage in originalMessages
                                  where originalMessage.IsDeferredMarkerMessage()
                                  select originalMessage.DeferredMessageSequenceNumber();

            return await busInstance
                .ReceiveDeferredMessages(sequenceNumbers)
                .ConfigureAwait(false);
        }

        private IncomingMessage BuildMessage(Message azureMessage, IList<Message> deferredMessages, IAzureServiceBusInstance busInstance)
        {
            Message markerMessage = null;
            if (azureMessage.IsDeferredMarkerMessage())
            {
                markerMessage = azureMessage;
                azureMessage = deferredMessages.Single(
                    message => message.SystemProperties.SequenceNumber == markerMessage.DeferredMessageSequenceNumber());
            }

            // TODO Pluggable serialiser
            var messageTypeNamesString = azureMessage.UserProperties[TransportHeaders.MessageTypeNames].ToString();
            // TODO Allocations review
            var messageTypeNames = messageTypeNamesString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var bodyJson = Encoding.UTF8.GetString(azureMessage.Body);
            var bodyType = typeMap.GetTypeByName(messageTypeNames[0]);

            var body = JsonConvert.DeserializeObject(bodyJson, bodyType);

            var headers = azureMessage
                .UserProperties
                .Where(property => !TransportHeaders.IsTransportHeader(property.Key))
                .Select(BuildHeader);

            return new IncomingMessage(
                id: azureMessage.MessageId,
                body: body,
                messageTypeNames: messageTypeNames,
                dequeuedUtc: DateTime.UtcNow,
                dequeuedCount: azureMessage.SystemProperties.DeliveryCount,
                lockExpiresUtc: azureMessage.SystemProperties.LockedUntilUtc,
                headers: new HeaderCollection(headers),
                providerData: new ProviderData(
                    busInstance,
                    azureMessage.SystemProperties.LockToken,
                    azureMessage.SystemProperties.SequenceNumber,
                    markerMessage?.SystemProperties.LockToken));
        }

        // TODO Should SMB header value be object rather than string?
        private static Header BuildHeader(KeyValuePair<string, object> azureHeader)
            => new Header(azureHeader.Key, azureHeader.Value?.ToString());
    }
}