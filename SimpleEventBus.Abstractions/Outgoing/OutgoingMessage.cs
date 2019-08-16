using System.Collections.Generic;
using System.Linq;

namespace SimpleEventBus.Abstractions.Outgoing
{
    public class OutgoingMessage
    {
        public OutgoingMessage(string id, object body, IEnumerable<string> messageTypeNames, IEnumerable<Header> headers = null, string specificReceivingEndpointName = null)
        {
            Id = id;
            Body = body;
            MessageTypeNames = messageTypeNames.ToList().AsReadOnly();
            Headers = new HeaderCollection(headers);
            SpecificReceivingEndpointName = specificReceivingEndpointName;
        }

        public string Id { get; }

        public object Body { get; }

        public IReadOnlyCollection<string> MessageTypeNames { get; }

        public HeaderCollection Headers { get; }

        public string SpecificReceivingEndpointName { get; }
    }
}
