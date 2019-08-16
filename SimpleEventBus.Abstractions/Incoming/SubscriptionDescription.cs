using System.Collections.Generic;
using System.Linq;

namespace SimpleEventBus.Abstractions.Incoming
{
    public class SubscriptionDescription
    {
        public SubscriptionDescription(string endpointName, IEnumerable<string> messageTypeNames)
        {
            MessageTypeNames = messageTypeNames.ToList().AsReadOnly();
            EndpointName = endpointName;
        }

        public IReadOnlyCollection<string> MessageTypeNames { get; }
        public string EndpointName { get; }

        public override int GetHashCode()
        {
            // System.HashCode uses a static random seed, meaning every process generates unique hashcodes.
            // We don't care about occasional changes in hash algorithm between .NET versions but unfortunately
            // there is still no way to turn off that static seed, making System.HashCode unsuitable for rule hashes.

            var hashCode = EndpointName == null ? 0 : EndpointName.GetHashCode();
            foreach (var messageTypeName in MessageTypeNames)
            {
                var currentItemCode = messageTypeName == null ? 0 : messageTypeName.GetHashCode();
                hashCode = CombineHashCodes(hashCode, currentItemCode);
            }

            return base.GetHashCode();
        }

        private static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }
    }
}
