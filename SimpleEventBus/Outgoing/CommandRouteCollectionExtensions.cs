using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Collections.ObjectModel;

namespace SimpleEventBus.Outgoing
{
    static class CommandRouteCollectionExtensions
    {
        public static string FindEndpointNameFor(this Collection<CommandRoute> routes, Type commandType)
        {
            foreach (var route in routes)
            {
                if (route.IsMatch(commandType))
                {
                    return route.ToEndpointName;
                }
            }

            throw new InvalidOperationException(
                $"No target endpoint has been configured for the command type \"{commandType.AssemblyQualifiedName}\".");
        }
    }
}
