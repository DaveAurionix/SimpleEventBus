using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Moq;
using System;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    static class MockMessageReceiverExtensions
    {
        public static void SetupReceiveThrowsTransientException(this Mock<IMessageReceiver> mock)
            => mock
                .Setup(m => m.ReceiveAsync(It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ThrowsAsync(new ServiceBusException(true));

        public static void VerifyReceiveCalledOnce(this Mock<IMessageReceiver> mock, TimeSpan withTimeout)
            => mock.Verify(m => m.ReceiveAsync(It.IsAny<int>(), withTimeout), Times.Once);

        public static void VerifyReceiveCalledTwice(this Mock<IMessageReceiver> mock, TimeSpan withTimeout)
            => mock.Verify(m => m.ReceiveAsync(It.IsAny<int>(), withTimeout), Times.Exactly(2));
    }
}
