using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace SimpleEventBus.AzureServiceBusTransport.Failover
{
    public class ActiveActiveFailoverStrategy : IFailoverStrategy
    {
        private readonly IAzureServiceBusInstance[] busInstances;
        private readonly ArrayPool<Exception> exceptionArrayPool = ArrayPool<Exception>.Shared;
        private readonly int busInstancesLength;

        public ActiveActiveFailoverStrategy(
            IEnumerable<IAzureServiceBusInstance> busInstances)
        {
            this.busInstances = busInstances.ToArray();
            busInstancesLength = this.busInstances.Length;
        }

        public async Task Send(IList<Message> azureMessages)
        {
            var failedInstanceCount = 0;
            var exceptions = exceptionArrayPool.Rent(busInstancesLength);

            try
            {
                for (var busIndex = 0; busIndex < busInstancesLength; busIndex++)
                {
                    var busInstance = busInstances[busIndex];

                    if (busInstance.IsCircuitBreakerTripped)
                    {
                        failedInstanceCount++;
                        continue;
                    }

                    exceptions[busIndex] = await busInstance.TrySend(azureMessages).ConfigureAwait(false);

                    if (exceptions[busIndex] != null)
                    {
                        failedInstanceCount++;
                    }
                }

                if (failedInstanceCount == busInstancesLength)
                {
                    // Deliberately returns exceptions from previous execution attempts.
                    throw new AggregateException(
                        "Could not send message as all bus instances are in a tripped state from previous errors, or experienced an error in this attempt.",
                        ((Exception[])exceptions.Clone())
                            .Where(exception => exception != null)
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
