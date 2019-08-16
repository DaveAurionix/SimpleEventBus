using Microsoft.Azure.ServiceBus;
using Moq;
using System.Collections.Generic;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    static class MockBusInstanceExtensions
    {
        public static void SetupTryReceiveMessagesReturnsNoMessages(this Mock<IAzureServiceBusInstance> mock)
            => mock
                .Setup(m => m.TryReceiveMessages(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Message>());

        public static void SetupTryReceiveMessagesReturns(this Mock<IAzureServiceBusInstance> mock, Message message)
            => mock
                .Setup(m => m.TryReceiveMessages(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Message>(new[] { message }));

        public static void SetupCircuitBreakerHasTripped(this Mock<IAzureServiceBusInstance> mock)
        {
            mock
                .Setup(m => m.TryReceiveMessages(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Message>());

            mock
                .Setup(m => m.IsCircuitBreakerTripped)
                .Returns(true);
        }

        public static void SetupTrySendReturnsException(this Mock<IAzureServiceBusInstance> mock, string exceptionMessage)
            => mock
                .Setup(m => m.TrySend(It.IsAny<IList<Message>>()))
                .ReturnsAsync(new ServiceBusException(true, exceptionMessage));

        public static void VerifyAbandonCalledOnce(this Mock<IAzureServiceBusInstance> mock, string expectedLockToken)
            => mock.Verify(m => m.Abandon(expectedLockToken), Times.Once);

        public static void VerifyCompleteCalledOnce(this Mock<IAzureServiceBusInstance> mock, string expectedLockToken)
            => mock.Verify(m => m.Complete(expectedLockToken), Times.Once);

        public static void VerifyTryReceiveMessagesCalledOnce(this Mock<IAzureServiceBusInstance> mock, bool waitIfQueueEmpty)
            => mock.Verify(m => m.TryReceiveMessages(It.IsAny<int>(), waitIfQueueEmpty), Times.Once);

        public static void VerifyTryReceiveMessagesCalledOnce(this Mock<IAzureServiceBusInstance> mock)
            => mock.Verify(m => m.TryReceiveMessages(It.IsAny<int>(), It.IsAny<bool>()), Times.Once);

        public static void VerifyTrySendCalledOnce(this Mock<IAzureServiceBusInstance> mock)
            => mock.Verify(m => m.TrySend(It.IsAny<IList<Message>>()), Times.Once);

        public static void VerifyTrySendCalledNever(this Mock<IAzureServiceBusInstance> mock)
            => mock.Verify(m => m.TrySend(It.IsAny<IList<Message>>()), Times.Never);

        public static void VerifyTryShutDownCalledOnce(this Mock<IAzureServiceBusInstance> mock)
            => mock.Verify(m => m.TryShutDown(), Times.Once);
    }
}
