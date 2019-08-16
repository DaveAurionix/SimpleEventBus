using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.Abstractions.Outgoing
{
    public interface IMessagePublisher
    {
        Task PublishEvent(object eventToPublish, IEnumerable<Header> additionalHeaders = null);
        Task PublishEvents(IEnumerable<object> eventsToPublish, IEnumerable<Header> additionalHeaders = null);
        Task SendCommand(object commandToSend, IEnumerable<Header> additionalHeaders = null);
        Task SendCommands(IEnumerable<object> commandsToSend, IEnumerable<Header> additionalHeaders = null);
    }
}
