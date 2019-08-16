using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.AzureServiceBusTransport.Failover;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
    internal class AzureServiceBusMessageSink : IMessageSink
    {
        private readonly IFailoverStrategy sendStrategy;

        public AzureServiceBusMessageSink(IFailoverStrategy sendStrategy)
        {
            this.sendStrategy = sendStrategy;
        }

        public async Task Sink(IEnumerable<OutgoingMessage> messages)
        {
            foreach (var batch in GetBatches(messages))
            {
                await sendStrategy
                    .Send(batch)
                    .ConfigureAwait(false);
            }
        }

        private static IEnumerable<IList<Message>> GetBatches(IEnumerable<OutgoingMessage> messages)
        {
            // Limit of 100 messages per operation (https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas)

            // TODO Batch size limit
            var batch = ArrayPool<Message>.Shared.Rent(100);
            try
            {
                var batchSize = 0;
                using (var enumerator = messages.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        batch[batchSize] = BuildMessage(enumerator.Current);
                        batchSize++;

                        if (batchSize == 100)
                        {
                            yield return new ArraySegment<Message>(batch, 0, 100);
                            batchSize = 0;
                        }
                    };
                }

                if (batchSize > 0)
                {
                    yield return new ArraySegment<Message>(batch, 0, batchSize);
                }
            }
            finally
            {
                ArrayPool<Message>.Shared.Return(batch, clearArray: false);
            }
        }

        private static Message BuildMessage(OutgoingMessage message)
        {
            // TODO Inject serialiser settings, e.g. for property type names.
            byte[] bodyBytes = null;

            if (message.Body != null)
            {
                bodyBytes = Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        message.Body));
            }

            var azureMessage = new Message
            {
                Body = bodyBytes,
                ContentType = "application/json",
                MessageId = message.Id
            };

            azureMessage.UserProperties.Add(
                TransportHeaders.MessageTypeNames,
                ";" + string.Join(";", message.MessageTypeNames) + ";");

            if (message.SpecificReceivingEndpointName != null)
            {
                azureMessage.UserProperties.Add(
                    TransportHeaders.SpecificEndpoint,
                    SubscriptionDescriptionExtensions.SafeSubscriptionName(
                        message.SpecificReceivingEndpointName));
            }

            foreach (var header in message.Headers)
            {
                azureMessage.UserProperties.Add(
                    header.HeaderName,
                    header.Value);
            }

            return azureMessage;
        }
    }
}
