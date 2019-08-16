using Microsoft.Azure.ServiceBus;
using Moq;
using SimpleEventBus.Abstractions.Incoming;
using System.Threading;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    static class MockSubscriptionInitialiserExtensions
    {
        public static void VerifyEnsureInitialisedCalledOnce(this Mock<ISubscriptionInitialiser> mock, SubscriptionDescription description, string connectionString)
            => mock.Verify(
                m => m.EnsureInitialised(
                    description,
                    connectionString,
                    It.IsAny<CancellationToken>()),
                Times.Once);

        public static void SetupEnsureInitialisedThrowsTransientException(this Mock<ISubscriptionInitialiser> mock, string connectionString)
            => mock.Setup(
                m => m.EnsureInitialised(
                    It.IsAny<SubscriptionDescription>(),
                    connectionString,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusException(true));

        public static void SetupEnsureInitialisedThrowsNonTransientException(this Mock<ISubscriptionInitialiser> mock, string connectionString)
            => mock.Setup(
                m => m.EnsureInitialised(
                    It.IsAny<SubscriptionDescription>(),
                    connectionString,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusException(false));
    }
}
