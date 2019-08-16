using SimpleEventBus.Abstractions.Incoming;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.Incoming
{
    public interface IIncomingPipeline
    {
        Task Initialise(CancellationToken cancellationToken);
        Task Process(IEnumerable<IncomingMessage> messages, CancellationToken cancellationToken);
    }
}
