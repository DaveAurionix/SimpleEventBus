using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using System;

namespace SimpleEventBus.UnitTests
{
    class IncomingMessageBuilder
    {
        readonly string id = Guid.NewGuid().ToString();
        object body;
        string bodyTypeName;
        DateTime dequeuedUtc = DateTime.UtcNow;
        DateTime lockExpiresUtc = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        int dequeuedCount = 1;
        readonly HeaderCollection headers = new HeaderCollection();

        public static IncomingMessageBuilder New()
            => new IncomingMessageBuilder();

        public IncomingMessageBuilder WithBody(object body)
        {
            this.body = body;
            bodyTypeName = FullNameTypeMap.Instance.GetNameForType(body.GetType());
            return this;
        }

        public IncomingMessageBuilder WithDequeuedCount(int dequeuedCount)
        {
            this.dequeuedCount = dequeuedCount;
            return this;
        }

        public IncomingMessageBuilder WithLockExpiry(DateTime dequeuedUtc, DateTime lockExpiresUtc)
        {
            this.lockExpiresUtc = lockExpiresUtc;
            this.dequeuedUtc = dequeuedUtc;
            return this;
        }

        public IncomingMessageBuilder WithHeader(string headerName, string value)
        {
            headers.Add(headerName, value);
            return this;
        }

        public IncomingMessage Build()
            => new IncomingMessage(id, body, new[] { bodyTypeName }, dequeuedUtc, lockExpiresUtc, dequeuedCount, headers);

        public static IncomingMessage BuildDefault()
            => New().Build();

        public static IncomingMessage BuildExpired()
            => New()
                .WithLockExpiry(
                    DateTime.UtcNow - TimeSpan.FromSeconds(40),
                    DateTime.UtcNow - TimeSpan.FromSeconds(30))
                .Build();

        public static IncomingMessage BuildWithBody(object body)
            => New().WithBody(body).Build();
    }
}
