using System;

namespace SimpleEventBus.Abstractions.Outgoing
{
    public class CommandRoute
    {
        private readonly Type commandType;

        public CommandRoute(Type commandType, string toEndpointName)
        {
            if (string.IsNullOrWhiteSpace(toEndpointName))
            {
                throw new ArgumentException("Endpoint must be specified.", nameof(toEndpointName));
            }

            this.commandType = commandType;
            ToEndpointName = toEndpointName;
        }

        public string ToEndpointName { get; }

        public bool IsMatch(Type commandTypeToCheck)
            => commandType == commandTypeToCheck;
    }
}
