using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.Abstractions.Outgoing
{
    public delegate Task OutgoingPipelineAction(IEnumerable<OutgoingMessage> messages);

    public interface IOutgoingBehaviour
    {
        Task Process(IEnumerable<OutgoingMessage> messages, OutgoingPipelineAction nextAction);
    }
}
