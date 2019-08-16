using Microsoft.Azure.ServiceBus;
using Moq;
using System.Collections.Generic;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    static class TopicClientExtensions
    {
        public static void SetupSendThrowsTransientException(this Mock<ITopicClient> mock)
            => mock
                .Setup(m => m.SendAsync(It.IsAny<IList<Message>>()))
                .ThrowsAsync(new ServiceBusException(true));
    }
}
