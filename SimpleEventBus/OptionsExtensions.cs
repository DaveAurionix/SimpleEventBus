using Microsoft.Extensions.DependencyInjection;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Incoming;
using SimpleEventBus.Outgoing;
using System;
using System.Linq;
using System.Reflection;

namespace SimpleEventBus
{
    public static class OptionsExtensions
    {
        private const string VisualStudioTestExplorerHostName = "testhost";

        public static Options UseFullNameTypeMap(this Options options)
        {
            options.Services.AddSingleton<ITypeMap>(FullNameTypeMap.Instance);
            return options;
        }

        public static Options UseSingletonHandlersIn(this Options options, Assembly handlersAssembly)
        {
            options.Services.AddSingletonHandlersFrom(handlersAssembly);
            return options;
        }

        public static Options UseTransientHandlersIn(this Options options, Assembly handlersAssembly)
        {
            options.Services.AddTransientHandlersFrom(handlersAssembly);
            return options;
        }

        public static Options UseConcurrencyLimit(this Options options, int maximumMessagesProcessedInParallel = 10)
        {
            options.Services.AddSingleton(
                sp => new ConcurrentMessageLimitingBehaviour(maximumMessagesProcessedInParallel));

            if (maximumMessagesProcessedInParallel > 0)
            {
                options.Services.AddSingleton<IIncomingBehaviour>(
                    sp => sp.GetRequiredService<ConcurrentMessageLimitingBehaviour>());
            }

            return options;
        }

        public static Options UseDynamicBatchSize(this Options options, double batchLockDurationRiskAttitude = 0.5)
        {
            options.Services.AddSingleton(
                sp => new BatchSizeGoverningBehaviour(
                    batchLockDurationRiskAttitude,
                    sp.GetLoggerOrDefault<BatchSizeGoverningBehaviour>()));

            options.Services.AddSingleton<IBatchSizeProvider>(
                sp => sp.GetRequiredService<BatchSizeGoverningBehaviour>());

            // This behaviour must also monitor failed message times so needs to appear very early in the chain.
            options.Services.AddSingletonAtStart<IIncomingBehaviour>(
                sp => sp.GetRequiredService<BatchSizeGoverningBehaviour>());

            return options;
        }

        public static Options UseFixedBatchSize(this Options options, int messagesInBatch)
        {
            options.Services.AddSingleton<IBatchSizeProvider>(
                sp => new FixedBatchSize(messagesInBatch));

            return options;
        }

        public static Options RouteCommandToEndpoint<TCommand>(this Options options, string toEndpointName)
        {
            options.CommandRoutes.Add(
                new CommandRoute(
                    typeof(TCommand),
                    toEndpointName));

            return options;
        }

        public static Options RouteCommandToSelf<TCommand>(this Options options)
            => RouteCommandToEndpoint<TCommand>(options, options.EndpointName);

        internal static Options UseDefaultsUnlessOverridden(this Options options)
        {
            if (string.IsNullOrWhiteSpace(options.EndpointName)
                || options.EndpointName == VisualStudioTestExplorerHostName)
            {
                throw new InvalidOperationException(
                    "Endpoint name is not set correctly.");
            }

            if (!options.IsConfigured<ITypeMap>())
            {
                options.UseFullNameTypeMap();
            }

            if (!options.IsConfigured<IBatchSizeProvider>())
            {
                options.UseDynamicBatchSize();
            }

            if (!options.IsConfigured<ConcurrentMessageLimitingBehaviour>())
            {
                options.UseConcurrencyLimit();
            }

            // Add this last to ensure that logging correlation id is added at the very start of the chain
            // TODO Better to use a priority enum?
            options.UseCorrelationId();

            options.Services.AddSingleton<OutgoingHeaderProviders>();
            options.Services.AddSingleton<IOutgoingBehaviour>(
                sp => new OutgoingHeadersBehaviour(
                    sp.GetRequiredService<OutgoingHeaderProviders>()));

            options.Services.AddSingleton(options.Retries);
            return options;
        }

        public static bool IsConfigured<T>(this Options options)
            => options.Services.Any(
                descriptor => descriptor.ServiceType == typeof(T));

        private static Options UseCorrelationId(this Options options)
        {
            options.Services.AddSingleton(
                sp => new CorrelationIdIncomingBehaviour(
                    sp.GetLoggerOrDefault<CorrelationIdIncomingBehaviour>()));

            options.Services.AddSingletonAtStart<IIncomingBehaviour>(
                sp => sp.GetRequiredService<CorrelationIdIncomingBehaviour>());

            options.Services.AddSingleton<IOutgoingHeaderProvider>(
                sp => sp.GetRequiredService<CorrelationIdIncomingBehaviour>());

            return options;
        }
    }
}
