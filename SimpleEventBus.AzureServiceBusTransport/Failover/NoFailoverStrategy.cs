using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace SimpleEventBus.AzureServiceBusTransport.Failover
{
    public class NoFailoverStrategy : IFailoverStrategy
    {
        readonly IAzureServiceBusInstance busInstance;

        public NoFailoverStrategy(IEnumerable<IAzureServiceBusInstance> busInstances)
        {
            busInstance = busInstances.First();
        }

        public async Task Send(IList<Message> azureMessages)
        {
            var exception = await busInstance.TrySend(azureMessages).ConfigureAwait(false);

            if (exception != null)
            {
                throw exception;
            }
        }
    }
}
