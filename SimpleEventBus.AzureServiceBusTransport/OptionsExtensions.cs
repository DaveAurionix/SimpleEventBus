using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.AzureServiceBusTransport;
using SimpleEventBus.AzureServiceBusTransport.Failover;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleEventBus
{
    public enum FailoverStrategy
    {
        None,
        ActivePassive,
        ActiveActive
    }

    public static class OptionsExtensions
    {
        public static Options UseAzureServiceBus(this Options options, AzureServiceBusTransportSettings settings)
        {
            if (!settings.ConnectionStrings.Any())
            {
                throw new InvalidOperationException(
                    $"No connection strings were set up in the {nameof(AzureServiceBusTransportSettings)} instance given to {nameof(UseAzureServiceBus)}.");
            }

            var connectionStringNumber = 0;
            foreach (var connectionString in settings.ConnectionStrings)
            {
                connectionStringNumber++;

                options.Services
                    .AddSingleton<IAzureServiceBusInstance>(
                        sp => new AzureServiceBusInstance(
                            new MessageReceiver(
                                connectionString,
                                EntityNameHelper.FormatSubscriptionPath(
                                    settings.SafeEffectiveTopicName,
                                    SubscriptionDescriptionExtensions.SafeSubscriptionName(options.EndpointName))),
                            new TopicClient(
                                connectionString,
                                settings.SafeEffectiveTopicName)
                            {
                                OperationTimeout = settings.SendTimeout
                            },
                            settings,
                            sp.GetLoggerOrDefault<AzureServiceBusInstance>(),
                            connectionStringNumber));
            }

            options
                .Services
                .AddSingleton(settings)
                .AddSingleton<ISubscriptionInitialiser>(
                    sp => new SubscriptionInitialiser(
                        settings,
                        sp.GetLoggerOrDefault<AzureServiceBusMessageSource>()))
                .AddSingleton(
                    sp => new AzureServiceBusMessageSink(
                        sp.GetFailoverStrategyOrDefault(
                            settings.ConnectionStrings)))
                .AddSingleton(
                    sp => new AzureServiceBusMessageSource(
                        settings,
                        sp.GetRequiredService<ISubscriptionInitialiser>(),
                        sp.GetServices<IAzureServiceBusInstance>(),
                        sp.GetRequiredService<ITypeMap>(),
                        sp.GetLoggerOrDefault<AzureServiceBusMessageSource>(),
                        sp.GetRequiredService<Options>().EndpointName))
                .AddSingleton<IMessageSink>(
                    sp => sp.GetRequiredService<AzureServiceBusMessageSink>())
                .AddScoped<IMessageSource>(
                    sp => sp.GetRequiredService<AzureServiceBusMessageSource>())
                .AddSingleton<NoFailoverStrategy>()
                .AddSingleton<ActiveActiveFailoverStrategy>()
                .AddSingleton<ActivePassiveFailoverStrategy>();

            return options;
        }

        public static Options UseFailoverStrategy<TFailoverStrategy>(this Options options)
            where TFailoverStrategy : class, IFailoverStrategy
        {
            options.Services.AddSingleton<IFailoverStrategy>(
                sp => sp.GetRequiredService<TFailoverStrategy>());
            return options;
        }

        public static Options Use(this Options options, FailoverStrategy strategy)
        {
            switch (strategy)
            {
                case FailoverStrategy.ActiveActive:
                    return options.UseFailoverStrategy<ActiveActiveFailoverStrategy>();
                case FailoverStrategy.ActivePassive:
                    return options.UseFailoverStrategy<ActivePassiveFailoverStrategy>();
                case FailoverStrategy.None:
                    return options.UseFailoverStrategy<NoFailoverStrategy>();
                default:
                    throw new NotSupportedException(
                        $"Failover strategy {strategy} unknown.");
            }
        }

        private static ILogger<T> GetLoggerOrDefault<T>(this IServiceProvider serviceProvider)
            => serviceProvider.GetService<ILogger<T>>() ?? NullLogger<T>.Instance;

        private static IFailoverStrategy GetFailoverStrategyOrDefault(this IServiceProvider serviceProvider, IEnumerable<string> connectionStrings)
        {
            var result = serviceProvider.GetService<IFailoverStrategy>();

            if (result != null)
            {
                return result;
            }

            if (connectionStrings.Count() <= 1)
            {
                return serviceProvider.GetRequiredService<NoFailoverStrategy>();
            }

            return serviceProvider.GetRequiredService<ActivePassiveFailoverStrategy>();
        }
    }
}
