using Microsoft.Extensions.DependencyInjection;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Outgoing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.Incoming
{
    class HandlerInvoker : IHandlerInvoker
    {
        private readonly IMessageSource messageSource;
        private readonly IServiceCollection registeredServices;
        private readonly ITypeMap typeMap;
        private readonly string endpointName;
        private readonly IOutgoingPipeline outgoingPipeline;
        private readonly HandlerBindingsDictionary handlerBindings = new HandlerBindingsDictionary();

        public HandlerInvoker(IMessageSource messageSource, IServiceCollection registeredServices, ITypeMap typeMap, string endpointName, IOutgoingPipeline outgoingPipeline)
        {
            this.messageSource = messageSource;
            this.registeredServices = registeredServices;
            this.typeMap = typeMap;
            this.endpointName = endpointName;
            this.outgoingPipeline = outgoingPipeline;
        }

        public async Task Initialise(CancellationToken cancellationToken)
        {
            var handledMessageTypeNames = new List<string>();

            foreach (var serviceDescriptor in registeredServices)
            {
                AddMessageTypesHandledBy(
                    serviceDescriptor.ServiceType,
                    handledMessageTypeNames);
            }

            await messageSource
                .EnsureSubscribed(
                    new SubscriptionDescription(
                        endpointName,
                        handledMessageTypeNames.Distinct()),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private void AddMessageTypesHandledBy(Type possibleHandlerType, List<string> handledMessageTypeNames)
        {
            foreach (var messageTypeHandled in AssemblyScanner.AllMessageTypesHandledBy(possibleHandlerType))
            {
                var mappedMessageTypeName = typeMap.GetNameForType(messageTypeHandled);
                handledMessageTypeNames.Add(mappedMessageTypeName);

                handlerBindings.AddBinding(
                    mappedMessageTypeName,
                    new HandlerBinding(possibleHandlerType, messageTypeHandled));
            }
        }

        public async Task Process(IncomingMessage message, Context context)
        {
            var messageTypeName = message.MessageTypeNames.First();

            if (!handlerBindings.ContainsKey(messageTypeName))
            {
                throw new InvalidOperationException(
                    $"Subscriber was handed a message with type-name {messageTypeName} but does not contain a handler for that type.");
            }

            // TODO Unit tests
            // TODO Integration tests for the scenario of multiple handlers for same message type
            if (handlerBindings[messageTypeName].Count == 1)
            {
                await handlerBindings[messageTypeName][0]
                    .Handle(message, context)
                    .ConfigureAwait(false);
                return;
            }

            var specificHandler = message.Headers.GetValueOrDefault("SpecificHandler");
            if (specificHandler != null)
            {
                var handlerBinding = handlerBindings[messageTypeName]
                    .Single(binding => binding.HandlerType.FullName == specificHandler);

                await handlerBinding
                    .Handle(message, context)
                    .ConfigureAwait(false);
                return;
            }

            // TODO Allocations review
            var messages = new List<OutgoingMessage>();
            foreach (var handlerBinding in handlerBindings[messageTypeName])
            {
                var outgoingHeaders = new[]
                {
                    new Header("SpecificHandler", handlerBinding.HandlerType.FullName)
                };

                messages.Add(
                    new OutgoingMessage(
                        Guid.NewGuid().ToString(),
                        message.Body,
                        message.MessageTypeNames,
                        outgoingHeaders,
                        endpointName));
            }

            await outgoingPipeline
                .Process(messages)
                .ConfigureAwait(false);
        }
    }
}
