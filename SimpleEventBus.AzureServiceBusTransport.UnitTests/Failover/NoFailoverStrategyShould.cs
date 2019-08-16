using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.AzureServiceBusTransport.Failover;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests.Failover
{
    [TestClass]
    public class NoFailoverStrategyShould
    {
        readonly Mock<IAzureServiceBusInstance> mockBusInstance1 = new Mock<IAzureServiceBusInstance>();
        readonly Mock<IAzureServiceBusInstance> mockBusInstance2 = new Mock<IAzureServiceBusInstance>();

        [TestMethod]
        public async Task SendToFirstClient()
        {
            var strategy = new NoFailoverStrategy(
                new[]
                {
                    mockBusInstance1.Object,
                    mockBusInstance2.Object
                });

            var messagesToSend = new List<Message>
            {
                new Message()
            };

            await strategy
                .Send(messagesToSend)
                .ConfigureAwait(false);

            mockBusInstance1.Verify(m => m.TrySend(messagesToSend));
        }

        // TODO Not send to second client
        // TODO Throw exception on first client failure
    }
}
