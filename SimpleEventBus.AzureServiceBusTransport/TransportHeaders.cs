using System;

namespace SimpleEventBus.AzureServiceBusTransport
{
    static class TransportHeaders
    {
        public const string SpecificEndpoint = "SpecificEndpoint";
        public const string MessageTypeNames = "MessageTypeNames";
        public const string DeferralReason = "DeferralReason";
        public const string DeferralReasonDetail = "DeferralReasonDetail";

        public static bool IsTransportHeader(string headerName)
            => string.Equals(headerName, SpecificEndpoint, StringComparison.OrdinalIgnoreCase)
            || string.Equals(headerName, MessageTypeNames, StringComparison.OrdinalIgnoreCase)
            || string.Equals(headerName, DeferralReason, StringComparison.OrdinalIgnoreCase)
            || string.Equals(headerName, DeferralReasonDetail, StringComparison.OrdinalIgnoreCase);
    }
}
