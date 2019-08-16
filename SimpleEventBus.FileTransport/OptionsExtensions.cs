using Microsoft.Extensions.DependencyInjection;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.FileTransport;

namespace SimpleEventBus
{
    public static class OptionsExtensions
    {
        public static Options UseFileBus(this Options options, string directoryPath)
        {
            options
                .Services
                .AddTransient(
                    sp => new FileBusConnection(directoryPath,
                        sp.GetRequiredService<ITypeMap>()))
                .AddSingleton<IMessageSink>(
                    sp => sp.GetRequiredService<FileBusConnection>())
                .AddScoped<IMessageSource>(
                    sp => sp.GetRequiredService<FileBusConnection>());

            return options;
        }
    }
}
