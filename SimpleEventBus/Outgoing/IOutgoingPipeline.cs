using SimpleEventBus.Abstractions.Outgoing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.Outgoing
{
    public interface IOutgoingPipeline
    {
        Task Process(IEnumerable<OutgoingMessage> messages);
    }
}
