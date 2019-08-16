using Microsoft.Azure.ServiceBus;
using System.Globalization;
using System.Text;

namespace SimpleEventBus.AzureServiceBusTransport
{
    static class MessageExtensions
    {
        public const string DeferredMessageMarkerLabel = "Deferred message";

        public static bool IsDeferredMarkerMessage(this Message message)
            => message.Label == DeferredMessageMarkerLabel;

        public static long DeferredMessageSequenceNumber(this Message message)
            => long.Parse(
                Encoding.UTF8.GetString(message.Body), CultureInfo.InvariantCulture);
    }
}
