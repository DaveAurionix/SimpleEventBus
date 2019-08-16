using System;

namespace SimpleEventBus.Testing
{
    public class CapturedMessage<TMessageType>
    {
        public CapturedMessage(string correlationId, string domainUnderTest, TMessageType message)
        {
            CorrelationId = correlationId;
            DomainUnderTest = domainUnderTest;
            Message = message;
            CapturedAtUtc = DateTime.UtcNow;
        }

        public string CorrelationId { get; }

        public string DomainUnderTest { get; }

        public TMessageType Message { get; }

        public DateTime CapturedAtUtc { get; }
    }
}
