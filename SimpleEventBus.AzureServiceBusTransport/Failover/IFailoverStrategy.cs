using Microsoft.Azure.ServiceBus;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.Failover
{
    public interface IFailoverStrategy
    {
        Task Send(IList<Message> azureMessages);
    }
}
