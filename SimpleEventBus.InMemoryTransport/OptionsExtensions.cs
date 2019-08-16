using Microsoft.Extensions.DependencyInjection;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.InMemoryTransport;

namespace SimpleEventBus
{
    public static class OptionsExtensions
    {
        public static Options UseInMemoryBus(this Options options)
        {
            options
                .Services
                .AddSingleton<InMemoryBus>()
                .AddTransient<InMemoryBusConnection>()
                .AddSingleton<IMessageSink>(sp => sp.GetRequiredService<InMemoryBusConnection>())
                .AddScoped<IMessageSource>(sp => sp.GetRequiredService<InMemoryBusConnection>());

            return options;
        }
    }
}
