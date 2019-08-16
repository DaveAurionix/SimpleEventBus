using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Incoming;
using SimpleEventBus.Outgoing;
using System;
using System.Reflection;

namespace SimpleEventBus
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSimpleEventBus(this IServiceCollection services, Action<Options> optionsCallback = null)
        {
            var options = new Options(services);

            optionsCallback?.Invoke(options);

            options.UseDefaultsUnlessOverridden();

            services
                .AddSingleton(options)
                .AddIncoming(options)
                .AddOutgoing(options);

            return services;
        }

        private static IServiceCollection AddIncoming(this IServiceCollection services, Options options)
            => services
                .AddSingleton<IIncomingPipeline>(
                    sp => new IncomingPipeline(
                        sp.GetRequiredService<IMessageSource>(),
                        sp.GetServices<IIncomingBehaviour>(),
                        sp.GetRequiredService<IServiceScopeFactory>(),
                        sp.GetRequiredService<IHandlerInvoker>(),
                        sp.GetLoggerOrDefault<IncomingPipeline>(),
                        sp.GetRequiredService<RetryOptions>()))
                .AddSingleton<IHandlerInvoker>(
                    sp => new HandlerInvoker(
                        sp.GetRequiredService<IMessageSource>(),
                        services,
                        sp.GetRequiredService<ITypeMap>(),
                        options.EndpointName,
                        sp.GetRequiredService<IOutgoingPipeline>()))
                .AddSingleton(
                    sp => new Endpoint(
                        sp.GetRequiredService<IMessageSource>(),
                        sp.GetRequiredService<IIncomingPipeline>(),
                        sp.GetLoggerOrDefault<Endpoint>(),
                        options.IsConcurrentFetchingEnabled,
                        sp.GetRequiredService<IBatchSizeProvider>()));

        private static IServiceCollection AddOutgoing(this IServiceCollection services, Options options)
            => services
                .AddSingleton(options.CommandRoutes)
                .AddSingleton<IOutgoingPipeline>(
                    sp => new OutgoingPipeline(
                        sp.GetServices<IOutgoingBehaviour>(),
                        sp.GetRequiredService<IMessageSink>(),
                        sp.GetLoggerOrDefault<OutgoingPipeline>()))
                .AddSingleton<IMessagePublisher, MessagePublisher>();

        public static IServiceCollection AddSingletonHandlersFrom(this IServiceCollection services, Assembly handlersAssembly)
        {
            foreach (var handlerType in AssemblyScanner.GetHandlersInAssembly(handlersAssembly))
            {
                services.AddSingleton(handlerType);
            }

            return services;
        }

        public static IServiceCollection AddTransientHandlersFrom(this IServiceCollection services, Assembly handlersAssembly)
        {
            foreach (var handlerType in AssemblyScanner.GetHandlersInAssembly(handlersAssembly))
            {
                services.AddTransient(handlerType);
            }

            return services;
        }

        public static IServiceCollection AddSingletonAtStart<TServiceType>(this IServiceCollection services, Func<IServiceProvider, TServiceType> implementationFactory)
            where TServiceType : class
        {
            services.Insert(0, ServiceDescriptor.Singleton(implementationFactory));
            return services;
        }

        public static ILogger<T> GetLoggerOrDefault<T>(this IServiceProvider serviceProvider)
            => serviceProvider.GetService<ILogger<T>>() ?? NullLogger<T>.Instance;
    }
}
