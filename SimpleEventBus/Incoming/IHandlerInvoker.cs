using SimpleEventBus.Abstractions.Incoming;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.Incoming
{
    public interface IHandlerInvoker
    {
        Task Initialise(CancellationToken cancellationToken);
        Task Process(IncomingMessage message, Context context);
    }
}
