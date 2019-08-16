using Microsoft.Azure.ServiceBus;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.Failover
{
    public class ActivePassiveFailoverStrategy : IFailoverStrategy
    {
        private readonly IAzureServiceBusInstance[] busInstances;
        private readonly ArrayPool<Exception> exceptionArrayPool = ArrayPool<Exception>.Shared;
        private readonly int busInstancesLength;

        public ActivePassiveFailoverStrategy(IEnumerable<IAzureServiceBusInstance> busInstances)
        {
            this.busInstances = busInstances.ToArray();
            busInstancesLength = this.busInstances.Length;
        }

        public async Task Send(IList<Message> azureMessages)
        {
            var exceptions = exceptionArrayPool.Rent(busInstancesLength);
            var failedInstancesCount = 0;

            try
            {
                for (var busIndex = 0; busIndex < busInstancesLength; busIndex++)
                {
                    var busInstance = busInstances[busIndex];

                    if (busInstance.IsCircuitBreakerTripped)
                    {
                        failedInstancesCount++;
                        continue;
                    }

                    exceptions[busIndex] = await busInstance.TrySend(azureMessages).ConfigureAwait(false);

                    if (exceptions[busIndex] == null)
                    {
                        return;
                    }

                    failedInstancesCount++;
                }

                if (failedInstancesCount == busInstancesLength)
                {
                    // Deliberately returns exceptions from previous execution attempts.
                    throw new AggregateException(
                        "Could not send message as all bus instances are in a tripped state from previous errors, or experienced an error in this attempt.",
                        ((Exception[])exceptions.Clone())
                            .Where(exception => exception!=null)
                            .Take(busInstancesLength)
                            .ToArray());
                }
            }
            finally
            {
                // Exceptions arrays are only returned when fully populated so no need to clear.
                exceptionArrayPool.Return(exceptions, false);
            }
        }
    }
}
