using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.Abstractions.Outgoing
{
    public interface IMessageSink
    {
        Task Sink(IEnumerable<OutgoingMessage> messages);
    }
}
