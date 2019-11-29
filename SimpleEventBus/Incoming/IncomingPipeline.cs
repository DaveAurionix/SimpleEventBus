using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.Incoming
{
    internal class IncomingPipeline : IIncomingPipeline
    {
        private readonly IIncomingBehaviour[] behaviours;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IMessageSource messageSource;
        private readonly IHandlerInvoker handlerInvoker;
        private readonly ILogger<IncomingPipeline> logger;
        private readonly RetryOptions retryOptions;
        private IncomingPipelineAction pipelineStartingAction;

        public IncomingPipeline(
            IMessageSource messageSource,
            IEnumerable<IIncomingBehaviour> behaviours,
            IServiceScopeFactory serviceScopeFactory,
            IHandlerInvoker handlerInvoker,
            ILogger<IncomingPipeline> logger,
            RetryOptions retryOptions)
        {
            this.behaviours = behaviours.ToArray();
            this.messageSource = messageSource;
            this.serviceScopeFactory = serviceScopeFactory;
            this.handlerInvoker = handlerInvoker;
            this.logger = logger;
            this.retryOptions = retryOptions;
        }

        public async Task Initialise(CancellationToken cancellationToken)
        {
            await handlerInvoker
                .Initialise(cancellationToken)
                .ConfigureAwait(false);

            pipelineStartingAction = BuildPipeline();
        }

        private IncomingPipelineAction BuildPipeline()
        {
            IncomingPipelineAction lastAction = handlerInvoker.Process;
            var nextAction = lastAction;

            foreach (var behaviour in behaviours.Reverse())
            {
                var capturedNextAction = nextAction;

                nextAction = (message, context)
                    => behaviour.Process(message, context, capturedNextAction);
            }

            foreach (var behaviour in behaviours)
            {
                logger.LogDebug($"Adding {behaviour.GetType().Name} to incoming pipeline.");
            }

            return nextAction;
        }

        public Task Process(IEnumerable<IncomingMessage> messages, CancellationToken cancellationToken)
            => Task
                .WhenAll(
                    messages.Select(
                        message => Process(message, cancellationToken)));

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Vast majority of exceptions will be application exceptions but these are thrown by third-party user code, we cannot identify all the types involved.")]
        private async Task Process(IncomingMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested
                || message.HasLockExpired)
            {
                await messageSource
                    .Abandon(message)
                    .ConfigureAwait(false);

                return;
            }

            using (var serviceScope = serviceScopeFactory.CreateScope())
            {
                var context = new Context(
                    serviceScope,
                    cancellationToken);
                try
                {
                    await pipelineStartingAction(message, context)
                        .ConfigureAwait(false);

                    await messageSource
                        .Complete(message)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    // Behaviours should have logged/recorded/responded to this exception.
                    if (message.DequeuedCount < retryOptions.MaximumImmediateAttempts)
                    {
                        await messageSource
                            .Abandon(message)
                            .ConfigureAwait(false);
                        return;
                    }

                    if (message.DequeuedCount - retryOptions.MaximumImmediateAttempts < retryOptions.MaximumDeferredAttempts)
                    {
                        await messageSource
                            .DeferUntil(message, DateTime.UtcNow + retryOptions.EffectiveDeferredRetryInterval, "Exception handling message", exception.ToString())
                            .ConfigureAwait(false);
                        return;
                    }

                    await messageSource
                        .DeadLetter(message, "Exception handling message", exception.ToString())
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
