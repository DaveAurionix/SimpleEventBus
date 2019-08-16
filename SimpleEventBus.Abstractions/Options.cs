using Microsoft.Extensions.DependencyInjection;
using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace SimpleEventBus
{
    public sealed class Options
    {
        public Options(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public Collection<CommandRoute> CommandRoutes { get; } = new Collection<CommandRoute>();

        public string EndpointName { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name;

        public bool IsConcurrentFetchingEnabled { get; set; } = true;

        public RetryOptions Retries { get; } = new RetryOptions();

        public Options UseEndpointName(string endpointName)
        {
            EndpointName = endpointName;
            return this;
        }

        public Options UseConcurrentFetching(bool isEnabled = true)
        {
            IsConcurrentFetchingEnabled = isEnabled;
            return this;
        }

        public Options UseRetries(Action<RetryOptions> retryOptions)
        {
            retryOptions(Retries);
            return this;
        }
    }
}
