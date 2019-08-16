using Microsoft.Extensions.DependencyInjection;
using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace SimpleEventBus.Incoming
{
    class HandlerBinding
    {
        private readonly Type handlerType;
        private readonly MethodInfo handlesMethod;

        public HandlerBinding(Type handlerType, Type messageType)
        {
            this.handlerType = handlerType;

            handlesMethod = handlerType.GetMethod(
                nameof(IHandles<object>.HandleMessage),
                new[] { messageType });

            if (handlesMethod == null)
            {
                throw new ArgumentException(
                    $"Handler type {handlerType.FullName} does not contain a method with the required signature.",
                    nameof(handlerType));
            }
        }

        public async Task Handle(IncomingMessage message, Context context)
        {
            var handlerInstance = context
                .ServiceScope
                .ServiceProvider
                .GetRequiredService(handlerType);

            try
            {
                var task = (Task)handlesMethod.Invoke(
                    handlerInstance,
                    new[] { message.Body });

                await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo
                    .Capture(exception.InnerException)
                    .Throw();
            }
        }

        public Type HandlerType => handlerType;
    }
}
