using SimpleEventBus.Abstractions.Incoming;

namespace SimpleEventBus.AzureServiceBusTransport
{
    static class SubscriptionDescriptionExtensions
    {
        public static string SafeSubscriptionName(this SubscriptionDescription subscription)
            => SafeSubscriptionName(subscription.EndpointName);

        public static string SafeSubscriptionName(string endpointName)
            => endpointName.Replace('\'', '-').Replace('_', '-');
    }
}
