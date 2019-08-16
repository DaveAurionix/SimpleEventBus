using System.Threading.Tasks;

namespace SimpleEventBus.Abstractions.Incoming
{
    public interface IHandles<TMessageType>
    {
        Task HandleMessage(TMessageType message);
    }
}
