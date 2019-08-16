using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleEventBus.Abstractions.Incoming
{
    public class IncomingMessage
    {
        public IncomingMessage(string id, object body, IEnumerable<string> messageTypeNames, DateTime dequeuedUtc, DateTime lockExpiresUtc, int dequeuedCount, HeaderCollection headers = null, object providerData = null)
        {
            Id = id;
            Body = body;
            MessageTypeNames = messageTypeNames.ToList().AsReadOnly();
            Headers = headers ?? new HeaderCollection();
            ProviderData = providerData;
            DequeuedUtc = dequeuedUtc;
            LockExpiresUtc = lockExpiresUtc;
            DequeuedCount = dequeuedCount;
        }

        public string Id { get; }

        public object Body { get; }

        public IReadOnlyCollection<string> MessageTypeNames { get; }

        public HeaderCollection Headers { get; }

        public object ProviderData { get; }

        public DateTime DequeuedUtc { get; }

        public int DequeuedCount { get; }

        public DateTime LockExpiresUtc { get; }

        public TimeSpan LockTime => LockExpiresUtc - DequeuedUtc;

        public TimeSpan RemainingLockTime => LockExpiresUtc - DateTime.UtcNow;

        public double LockTimeFractionRemaining => Math.Max(0.0, RemainingLockTime.TotalSeconds) / LockTime.TotalSeconds;

        public bool HasLockExpired => LockExpiresUtc < DateTime.UtcNow;

        public static IncomingMessage FromOutgoing(OutgoingMessage message, DateTime newDequeuedUtc, DateTime newLockExpiresUtc, int dequeuedCount)
            => new IncomingMessage(message.Id, message.Body, message.MessageTypeNames, newDequeuedUtc, newLockExpiresUtc, dequeuedCount, headers: message.Headers);

        public readonly static IReadOnlyCollection<IncomingMessage> EmptyReadOnlyCollection = new List<IncomingMessage>().AsReadOnly();
    }
}
