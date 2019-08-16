using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SimpleEventBus.InMemoryTransport
{
    class QueuedMessage
    {
        private int dequeuedCount;

        public QueuedMessage(string id, object body, IReadOnlyCollection<string> messageTypeNames, HeaderCollection headers)
        {
            Id = id;
            Body = body;
            MessageTypeNames = messageTypeNames;
            Headers = headers;
        }

        public string Id { get; }

        public object Body { get; }

        public IReadOnlyCollection<string> MessageTypeNames { get; }

        public HeaderCollection Headers { get; }

        public int DequeuedCount => dequeuedCount;

        public static QueuedMessage FromOutgoing(OutgoingMessage source)
            => new QueuedMessage(source.Id, source.Body, source.MessageTypeNames, source.Headers);

        public IncomingMessage DequeueToIncoming(TimeSpan lockTime)
        {
            // TODO Unit test that dequeuedcount is incremented.
            var currentDequeuedCount = Interlocked.Increment(ref dequeuedCount);

            return new IncomingMessage(Id, Body, MessageTypeNames, DateTime.UtcNow, DateTime.UtcNow + lockTime, currentDequeuedCount, Headers, null);
        }
    }
}
