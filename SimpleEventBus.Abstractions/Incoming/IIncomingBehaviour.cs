using System.Threading.Tasks;

namespace SimpleEventBus.Abstractions.Incoming
{
    public delegate Task IncomingPipelineAction(IncomingMessage message, Context context);

    public interface IIncomingBehaviour
    {
        Task Process(IncomingMessage message, Context context, IncomingPipelineAction nextAction);
    }
}
