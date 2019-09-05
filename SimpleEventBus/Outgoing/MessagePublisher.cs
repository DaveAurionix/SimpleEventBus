using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventBus.Outgoing
{
    // TODO Naming review - is this class an event publisher, a command sender or something else?
    sealed class MessagePublisher : IMessagePublisher
    {
        private readonly IOutgoingPipeline outgoingPipeline;
        private readonly ITypeMap typeMap;
        private readonly Collection<CommandRoute> commandRoutes;

        public MessagePublisher(IOutgoingPipeline outgoingPipeline, ITypeMap typeMap, Collection<CommandRoute> commandRoutes)
        {
            this.outgoingPipeline = outgoingPipeline;
            this.typeMap = typeMap;
            this.commandRoutes = commandRoutes;
        }

        public Task PublishEvent(object eventToPublish, IEnumerable<Header> additionalHeaders = null)
            => outgoingPipeline.Process(
                new[] {
                    MapEventToMessage(eventToPublish, additionalHeaders)
                });

        public Task PublishEvents(IEnumerable<object> eventsToPublish, IEnumerable<Header> additionalHeaders = null)
            => outgoingPipeline.Process(
                eventsToPublish.Select(
                    eventToPublish => MapEventToMessage(eventToPublish, additionalHeaders)));

        private OutgoingMessage MapEventToMessage(object eventToPublish, IEnumerable<Header> additionalHeaders = null)
            => new OutgoingMessage(
                Guid.NewGuid().ToString(),
                eventToPublish,
                GetMessageTypes(eventToPublish),
                additionalHeaders);

        public Task SendCommand(object commandToSend, IEnumerable<Header> additionalHeaders = null)
            => outgoingPipeline.Process(
                new[] {
                    MapCommandToMessage(commandToSend, additionalHeaders)
                });
                

        public Task SendCommands(IEnumerable<object> commandsToSend, IEnumerable<Header> additionalHeaders = null)
            => outgoingPipeline.Process(
                commandsToSend.Select(
                    commandToSend => MapCommandToMessage(commandToSend, additionalHeaders)));

        private OutgoingMessage MapCommandToMessage(object commandToSend, IEnumerable<Header> additionalHeaders = null)
        {
            var endpointName = commandRoutes
                .FindEndpointNameFor(
                    commandToSend.GetType());

            return new OutgoingMessage(
                Guid.NewGuid().ToString(),
                commandToSend,
                GetMessageTypes(commandToSend),
                specificReceivingEndpointName: endpointName,
                headers: additionalHeaders);
        }

        private List<string> GetMessageTypes(object message)
        {
            var bodyType = typeMap
                .GetNameForType(
                    message.GetType());

            return new List<string>
            {
                bodyType
            };
        }
    }
}
