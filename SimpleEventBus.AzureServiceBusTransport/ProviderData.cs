using Microsoft.Azure.ServiceBus;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport
{
    // TODO Avoid allocation?
    public class ProviderData
    {
        private readonly IAzureServiceBusInstance busInstance;
        private readonly long sequenceNumber;
        private readonly string markerMessageLockToken;

        public ProviderData(IAzureServiceBusInstance busInstance, string lockToken, long sequenceNumber, string markerMessageLockToken)
        {
            this.busInstance = busInstance;
            LockToken = lockToken;
            this.sequenceNumber = sequenceNumber;
            this.markerMessageLockToken = markerMessageLockToken;
        }

        public async Task Abandon()
        {
            await busInstance.Abandon(LockToken).ConfigureAwait(false);

            if (markerMessageLockToken != null)
            {
                await busInstance.Abandon(markerMessageLockToken).ConfigureAwait(false);
            }
        }

        public async Task Complete()
        {
            await busInstance.Complete(LockToken).ConfigureAwait(false);

            if (markerMessageLockToken != null)
            {
                // TODO Possible to complete one message but not the other, then fetch will deadlock.
                await busInstance.Complete(markerMessageLockToken).ConfigureAwait(false);
            }
        }

        public async Task DeadLetter(string deadLetterReason, string deadLetterReasonDetail)
        {
            await busInstance
                .DeadLetter(LockToken, deadLetterReason, deadLetterReasonDetail)
                .ConfigureAwait(false);

            if (markerMessageLockToken != null)
            {
                // TODO Possible to complete one message but not the other, then fetch will deadlock.
                await busInstance
                    .Complete(markerMessageLockToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task DeferOnSameBusInstanceUntil(DateTime deferUntilUtc, string deferralReason, string deferralReasonDetail, string thisEndpointName)
        {
            // If the bus instance becomes faulty, the original message will fail and be retried, we shouldn't send the scheduled marker message to one bus and defer the original message on a different bus.
            
            // TODO Unit test
            var exception = await busInstance
                .TryScheduleMessage(
                    CreateDeferredMessageMarker(sequenceNumber, thisEndpointName),
                    deferUntilUtc)
                .ConfigureAwait(false);

            if (exception != null)
            {
                throw exception;
            }

            await busInstance
                .Defer(LockToken, deferralReason, deferralReasonDetail)
                .ConfigureAwait(false);

            if (markerMessageLockToken != null)
            {
                await busInstance
                    .Complete(markerMessageLockToken)
                    .ConfigureAwait(false);
            }
        }

        public string LockToken { get; }

        private static Message CreateDeferredMessageMarker(long deferredMessageSequenceNumber, string thisEndpointName)
            => new Message()
            {
                // For support purposes we store the number as a string - we can human-read it from Azure Storage Explorer when investigating issues.
                Body = Encoding.UTF8.GetBytes(deferredMessageSequenceNumber.ToString(CultureInfo.InvariantCulture)),
                ContentType = "text/plain",
                Label = MessageExtensions.DeferredMessageMarkerLabel,
                UserProperties =
                {
                    { TransportHeaders.SpecificEndpoint, thisEndpointName }
                }
            };
    }
}
