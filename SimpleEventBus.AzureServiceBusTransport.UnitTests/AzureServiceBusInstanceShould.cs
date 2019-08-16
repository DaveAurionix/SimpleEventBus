using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    [TestClass]
    public class AzureServiceBusInstanceShould
    {
        // TODO unit test coverage review

        private readonly Mock<IMessageReceiver> mockMessageReceiver = new Mock<IMessageReceiver>();
        private readonly Mock<ITopicClient> mockTopicClient = new Mock<ITopicClient>();
        private AzureServiceBusInstance instance;
        private AzureServiceBusTransportSettings settings;

        [TestInitialize]
        public void Setup()
        {
            settings = new AzureServiceBusTransportSettings
            {
                ConnectionStrings = new[]
                {
                    "connection 1",
                    "connection 2"
                },
                ReadTimeout = TimeSpan.FromSeconds(1),
                LongWaitReadTimeout = TimeSpan.FromSeconds(3),
                BackoffDelayForFaultyConnection = TimeSpan.FromSeconds(5),
                BackoffDelayIfAllConnectionsFaulty = TimeSpan.FromSeconds(7)
            };

            instance = new AzureServiceBusInstance(
                mockMessageReceiver.Object,
                mockTopicClient.Object,
                settings,
                NullLogger<AzureServiceBusInstance>.Instance,
                1);
        }

        [TestMethod]
        public async Task TryReceiveMessagesWithShortWaitTimeOnFirstCallToDetectFailureEarly()
        {
            await instance.TryReceiveMessages(32, true).ConfigureAwait(false);
            mockMessageReceiver.VerifyReceiveCalledOnce(settings.ReadTimeout);
        }

        [TestMethod]
        public async Task ReceiveMessagesWithLongWaitTimeOnSubsequentCallsToMinimiseServiceBusRequestsInHappyPath()
        {
            await instance.TryReceiveMessages(32, true).ConfigureAwait(false);
            await instance.TryReceiveMessages(32, true).ConfigureAwait(false);
            mockMessageReceiver.VerifyReceiveCalledOnce(settings.LongWaitReadTimeout);
        }

        [TestMethod]
        public async Task ReceiveMessagesWithShortWaitTimeOnSubsequentCallsIfCallerDoesNotWantToWait()
        {
            await instance.TryReceiveMessages(32, true).ConfigureAwait(false);
            await instance.TryReceiveMessages(32, false).ConfigureAwait(false);
            mockMessageReceiver.VerifyReceiveCalledTwice(settings.ReadTimeout);
        }

        [TestMethod]
        public async Task ReceiveMessagesWithShortWaitTimeOnSubsequentCallsIfFirstCallFaultedEvenIfCallerIsHappyToWaitLonger()
        {
            mockMessageReceiver.SetupReceiveThrowsTransientException();
            await instance.TryReceiveMessages(32, true).ConfigureAwait(false);
            await instance.TryReceiveMessages(32, true).ConfigureAwait(false);
            mockMessageReceiver.VerifyReceiveCalledTwice(settings.ReadTimeout);
        }

        [TestMethod]
        public async Task ReturnNoMessagesIfConnectionFaults()
        {
            mockMessageReceiver.SetupReceiveThrowsTransientException();
            var result = await instance.TryReceiveMessages(32, false).ConfigureAwait(false);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task TripCircuitBreakerIfConnectionFaultsWhenReceivingMessages()
        {
            mockMessageReceiver.SetupReceiveThrowsTransientException();
            await instance.TryReceiveMessages(32, false).ConfigureAwait(false);
            Assert.IsTrue(instance.IsCircuitBreakerTripped);
        }

        [TestMethod]
        public async Task ResetCircuitBreakerAfterSingleConnectionBackoffPeriod()
        {
            mockMessageReceiver.SetupReceiveThrowsTransientException();
            await instance.TryReceiveMessages(32, true).ConfigureAwait(false);

            await Task
                .Delay(settings.BackoffDelayForFaultyConnection + TimeSpan.FromSeconds(1))
                .ConfigureAwait(false);

            Assert.IsFalse(instance.IsCircuitBreakerTripped);
        }

        [TestMethod]
        public async Task ReturnExceptionIfConnectionFaultsWhenSendingMessages()
        {
            mockTopicClient.SetupSendThrowsTransientException();
            var result = await instance.TrySend(Array.Empty<Message>()).ConfigureAwait(false);

            Assert.IsInstanceOfType(result, typeof(ServiceBusException));
        }

        [TestMethod]
        public async Task TripCircuitBreakerIfConnectionFaultsWhenSendingMessages()
        {
            mockTopicClient.SetupSendThrowsTransientException();
            await instance.TrySend(Array.Empty<Message>()).ConfigureAwait(false);
            Assert.IsTrue(instance.IsCircuitBreakerTripped);
        }

        [TestMethod]
        public async Task CloseTopicClientOnShutDown()
        {
            await instance.TryShutDown().ConfigureAwait(false);
            mockTopicClient.Verify(m => m.CloseAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CloseMessageReceiverOnShutDown()
        {
            await instance.TryShutDown().ConfigureAwait(false);
            mockMessageReceiver.Verify(m => m.CloseAsync(), Times.Once);
        }

        [TestMethod]
        public async Task NotThrowExceptionIfTopicClientThrowsOnShutDown()
        {
            mockTopicClient.Setup(m => m.CloseAsync()).ThrowsAsync(new ServiceBusException(true));
            await instance.TryShutDown().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NotThrowExceptionIfMessageReceiverThrowsOnShutDown()
        {
            mockMessageReceiver.Setup(m => m.CloseAsync()).ThrowsAsync(new ServiceBusException(true));
            await instance.TryShutDown().ConfigureAwait(false);
        }
    }
}
