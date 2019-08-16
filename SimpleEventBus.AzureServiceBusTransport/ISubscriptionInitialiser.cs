using SimpleEventBus.Abstractions.Incoming;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport
{
    public interface ISubscriptionInitialiser
    {
        Task EnsureInitialised(SubscriptionDescription subscription, string connectionString, CancellationToken cancellationToken);
    }
}
