using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using SimpleEventBus.AzureServiceBusTransport.Failover;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport
{
    class AzureServiceBusInstance : IAzureServiceBusInstance
    {
        private readonly IMessageReceiver messageReceiver;
        private readonly ITopicClient topicClient;
        private readonly AzureServiceBusTransportSettings settings;
        private readonly ILogger logger;
        private readonly int connectionStringNumber;
        private DateTime? circuitBreakerLastTripped;
        private bool detectFailureFast;
        private readonly static IList<Message> emptyList = new List<Message>().AsReadOnly();

        public AzureServiceBusInstance(IMessageReceiver messageReceiver, ITopicClient topicClient, AzureServiceBusTransportSettings settings, ILogger logger, int connectionStringNumber)
        {
            this.messageReceiver = messageReceiver;
            this.topicClient = topicClient;
            this.settings = settings;
            this.logger = logger;
            this.connectionStringNumber = connectionStringNumber;
            detectFailureFast = true;
        }

        public async Task TryShutDown()
        {
            try
            {
                await Task
                    .WhenAll(
                        topicClient.CloseAsync(),
                        messageReceiver.CloseAsync())
                    .ConfigureAwait(false);
            }
            catch (ServiceBusException exception)
            {
                logger.LogError(
                    exception,
                    $"Exception when shutting down connection {connectionStringNumber}: {exception.Message}");
            }
        }

        public Task<IList<Message>> TryReceiveMessages(int maximumMessagesToReturn, bool waitIfQueueEmpty)
            => FailoverExceptions.Try(
                async () =>
                {
                    var operationTimeout = (detectFailureFast || !waitIfQueueEmpty) ? settings.ReadTimeout : settings.LongWaitReadTimeout;

                    var results = await messageReceiver
                        .ReceiveAsync(maximumMessagesToReturn, operationTimeout)
                        .ConfigureAwait(false);

                    if (results == null)
                    {
                        results = emptyList;
                    }

                    detectFailureFast = false;

                    return results;
                },
                exception =>
                {
                    logger.LogWarning(
                        exception,
                        $"Exception waiting for messages using connection {connectionStringNumber}: {exception.Message}");
                    circuitBreakerLastTripped = DateTime.UtcNow;
                    detectFailureFast = true;

                    return emptyList;
                });

        public Task<IList<Message>> ReceiveDeferredMessages(IEnumerable<long> sequenceNumbers)
            => messageReceiver.ReceiveDeferredMessageAsync(sequenceNumbers);

        public bool IsCircuitBreakerTripped
            => circuitBreakerLastTripped.HasValue
                && circuitBreakerLastTripped >= DateTime.UtcNow - settings.BackoffDelayForFaultyConnection;

        public Task<Exception> TrySend(IList<Message> messages)
            => FailoverExceptions.Try(
                async () =>
                {
                    await topicClient.SendAsync(messages).ConfigureAwait(false);
                    return null;
                },
                exception =>
                {
                    logger.LogWarning(
                        exception,
                        $"Exception sending messages using connection {connectionStringNumber}: {exception.Message}");
                    circuitBreakerLastTripped = DateTime.UtcNow;
                    return exception;
                });

        public Task Abandon(string lockToken)
            => messageReceiver.AbandonAsync(lockToken);

        public Task Complete(string lockToken)
            => messageReceiver.CompleteAsync(lockToken);

        public Task DeadLetter(string lockToken, string deadLetterReason, string deadLetterReasonDetail)
            => messageReceiver.DeadLetterAsync(lockToken, deadLetterReason, deadLetterReasonDetail);

        public Task<Exception> TryScheduleMessage(Message message, DateTime scheduledTimeUtc)
            => FailoverExceptions.Try(
                async () =>
                {
                    await topicClient.ScheduleMessageAsync(message, scheduledTimeUtc).ConfigureAwait(false);
                    return null;
                },
                exception =>
                {
                    logger.LogWarning(
                        exception,
                        $"Exception sending scheduled messages using connection {connectionStringNumber}: {exception.Message}");
                    circuitBreakerLastTripped = DateTime.UtcNow;
                    return exception;
                });

        public Task Defer(string lockToken, string deferralReason, string deferralReasonDetail)
            => messageReceiver
                .DeferAsync(lockToken, new Dictionary<string, object>
                {
                    { TransportHeaders.DeferralReason, deferralReason },
                    { TransportHeaders.DeferralReasonDetail, deferralReasonDetail }
                });
    }
}
