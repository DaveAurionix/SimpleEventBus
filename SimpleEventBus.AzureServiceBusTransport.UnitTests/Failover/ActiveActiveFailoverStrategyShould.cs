using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.AzureServiceBusTransport.Failover;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests.Failover
{
    [TestClass]
    public class ActiveActiveFailoverStrategyShould
    {
        readonly Mock<IAzureServiceBusInstance> mockBusInstance1 = new Mock<IAzureServiceBusInstance>();
        readonly Mock<IAzureServiceBusInstance> mockBusInstance2 = new Mock<IAzureServiceBusInstance>();
        IList<Message> messagesToSend;
        ActiveActiveFailoverStrategy strategy;

        [TestInitialize]
        public void Setup()
        {
            strategy = new ActiveActiveFailoverStrategy(
                new[]
                {
                    mockBusInstance1.Object,
                    mockBusInstance2.Object
                });

            messagesToSend = new List<Message>
            {
                new Message()
            };
        }

        [TestMethod]
        public async Task SendToAllClients()
        {
            await strategy.Send(messagesToSend).ConfigureAwait(false);

            mockBusInstance1.VerifyTrySendCalledOnce();
            mockBusInstance2.VerifyTrySendCalledOnce();
        }

        [TestMethod]
        public async Task SkipClientsThatHavePreviouslyFaulted()
        {
            mockBusInstance1.SetupCircuitBreakerHasTripped();
            await strategy.Send(messagesToSend).ConfigureAwait(false);

            mockBusInstance1.VerifyTrySendCalledNever();
            mockBusInstance2.VerifyTrySendCalledOnce();
        }

        [TestMethod]
        public async Task SkipClientsThatFailOnThisCall()
        {
            mockBusInstance1.SetupTrySendReturnsException("Something bad happened");
            await strategy.Send(messagesToSend).ConfigureAwait(false);

            mockBusInstance1.VerifyTrySendCalledOnce();
            mockBusInstance2.VerifyTrySendCalledOnce();
        }

        [TestMethod]
        public async Task ThrowExceptionIfAllClientsFailToSend()
        {
            mockBusInstance1.SetupTrySendReturnsException("Something bad happened");
            mockBusInstance2.SetupTrySendReturnsException("Something bad happened");

            await Assert
                .ThrowsExceptionAsync<AggregateException>(
                    () => strategy.Send(messagesToSend))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ThrowExceptionIfAllClientsAreInAFaultedState()
        {
            mockBusInstance1.SetupCircuitBreakerHasTripped();
            mockBusInstance2.SetupCircuitBreakerHasTripped();

            await Assert
                .ThrowsExceptionAsync<AggregateException>(
                    () => strategy.Send(messagesToSend))
                .ConfigureAwait(false);

            mockBusInstance1.VerifyTrySendCalledNever();
            mockBusInstance2.VerifyTrySendCalledNever();
        }
    }
}
