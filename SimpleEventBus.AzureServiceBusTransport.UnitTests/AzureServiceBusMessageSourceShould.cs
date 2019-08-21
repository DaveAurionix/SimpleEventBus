using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    [TestClass]
    public class AzureServiceBusMessageSourceShould
    {
        const string EndpointName = "My.Endpoint";

        readonly Mock<IAzureServiceBusInstance> busInstance1 = new Mock<IAzureServiceBusInstance>();
        readonly Mock<IAzureServiceBusInstance> busInstance2 = new Mock<IAzureServiceBusInstance>();

        readonly Mock<ISubscriptionInitialiser> mockSubscriptionInitialiser = new Mock<ISubscriptionInitialiser>();

        AzureServiceBusMessageSource source;
        IncomingMessage incomingMessage;
        AzureServiceBusTransportSettings settings;
        Message azureMessage;

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

            source = new AzureServiceBusMessageSource(
                settings,
                mockSubscriptionInitialiser.Object,
                new[]
                {
                    busInstance1.Object,
                    busInstance2.Object
                },
                new FullNameTypeMap(),
                NullLogger<AzureServiceBusMessageSource>.Instance,
                EndpointName);

            azureMessage = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("message body")),
                UserProperties =
                {
                    { "TestHeader", "TestValue" },
                    { "MessageTypeNames", ";System.String;"}
                }
            };

            azureMessage.SystemProperties.SetPrivateProperty("SequenceNumber", 1L);
            azureMessage.SystemProperties.SetPrivateProperty("LockTokenGuid", Guid.NewGuid());
            azureMessage.SystemProperties.SetPrivateProperty("DeliveryCount", 22);

            busInstance1.SetupTryReceiveMessagesReturns(azureMessage);
            busInstance2.SetupTryReceiveMessagesReturns(azureMessage);

            incomingMessage = new IncomingMessage(
                Guid.NewGuid().ToString(),
                "message body",
                new[] { "System.String" },
                DateTime.UtcNow,
                DateTime.UtcNow + TimeSpan.FromSeconds(10),
                1,
                providerData: new ProviderData(busInstance1.Object, "lock token", 1234, null));
        }

       [TestMethod]
        public async Task TryToShutDownAllBusInstancesWhenClosed()
        {
            await source.Close().ConfigureAwait(false);
            busInstance1.VerifyTryShutDownCalledOnce();
            busInstance2.VerifyTryShutDownCalledOnce();
        }

        [TestMethod]
        public async Task AbandonMessageWithLockTokenWhenAbandoningMessage()
        {
            await source.Abandon(incomingMessage).ConfigureAwait(false);
            busInstance1.VerifyAbandonCalledOnce("lock token");
        }

        [TestMethod]
        public async Task CompleteMessageWithLockTokenWhenCompletingMessage()
        {
            await source.Complete(incomingMessage).ConfigureAwait(false);
            busInstance1.VerifyCompleteCalledOnce("lock token");
        }

        [TestMethod]
        public async Task DelegateSubscriptionInitialisationForEachConnectionString()
        {
            var description = new SubscriptionDescription(EndpointName, Enumerable.Empty<string>());
            await source.EnsureSubscribed(description, CancellationToken.None).ConfigureAwait(false);

            mockSubscriptionInitialiser.VerifyEnsureInitialisedCalledOnce(description, "connection 1");
            mockSubscriptionInitialiser.VerifyEnsureInitialisedCalledOnce(description, "connection 2");
        }

        [TestMethod]
        public async Task DelegateSubscriptionInitialisationForEachConnectionStringEvenIfOneThrowsATransientException()
        {
            mockSubscriptionInitialiser.SetupEnsureInitialisedThrowsTransientException("connection 1");

            var description = new SubscriptionDescription(EndpointName, Enumerable.Empty<string>());
            await source.EnsureSubscribed(description, CancellationToken.None).ConfigureAwait(false);

            mockSubscriptionInitialiser.VerifyEnsureInitialisedCalledOnce(description, "connection 1");
            mockSubscriptionInitialiser.VerifyEnsureInitialisedCalledOnce(description, "connection 2");
        }

        [TestMethod]
        public async Task FailToInitialiseSubscriptionIfAnyConnectionStringThrowsANonTransientException()
        {
            mockSubscriptionInitialiser.SetupEnsureInitialisedThrowsNonTransientException("connection 1");

            var description = new SubscriptionDescription(EndpointName, Enumerable.Empty<string>());

            var exception = await Assert
                .ThrowsExceptionAsync<ServiceBusException>(
                    () => source.EnsureSubscribed(description, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual("Exception of type 'Microsoft.Azure.ServiceBus.ServiceBusException' was thrown.", exception.Message);
        }

        [TestMethod]
        public async Task FailToInitialiseSubscriptionIfAllConnectionStringsThrowAnyException()
        {
            mockSubscriptionInitialiser.SetupEnsureInitialisedThrowsTransientException("connection 1");

            mockSubscriptionInitialiser.SetupEnsureInitialisedThrowsTransientException("connection 2");

            var description = new SubscriptionDescription(EndpointName, Enumerable.Empty<string>());

            var exception = await Assert
                .ThrowsExceptionAsync<AggregateException>(
                    () => source.EnsureSubscribed(description, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual("No Service Bus connections could be initialised. (Exception of type 'Microsoft.Azure.ServiceBus.ServiceBusException' was thrown.) (Exception of type 'Microsoft.Azure.ServiceBus.ServiceBusException' was thrown.)", exception.Message);
        }

        [TestMethod]
        public async Task ReceiveMessagesFromPrimaryConnectionAndWaitToMinimiseServiceBusRequests()
        {
            await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            busInstance1.VerifyTryReceiveMessagesCalledOnce(waitIfQueueEmpty: true);
        }

        [TestMethod]
        public async Task CheckSecondaryConnectionsWithAShortWaitTimeIfPrimaryConnectionHasNoMessagesInCaseSenderIsLockedOntoNonPrimaryBusAndQueueHasBuiltUp()
        {
            busInstance1.SetupTryReceiveMessagesReturnsNoMessages();
            await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            busInstance2.VerifyTryReceiveMessagesCalledOnce(waitIfQueueEmpty: false);
        }

        [TestMethod]
        public async Task SkipAnyConnectionThatHasPreviouslyFaulted()
        {
            busInstance1.SetupCircuitBreakerHasTripped();
            await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            busInstance2.VerifyTryReceiveMessagesCalledOnce();
        }

        [TestMethod]
        public async Task CheckSecondaryConnectionsWithALongWaitTimeIfPrimaryConnectionIsFaultyAndFirstShortWaitTimeCallToSecondaryDidNotFault()
        {
            busInstance1.SetupCircuitBreakerHasTripped();
            await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            busInstance2.VerifyTryReceiveMessagesCalledOnce(waitIfQueueEmpty: true);
        }

        [TestMethod]
        public async Task DelayByAllConnectionBackoffPeriodIfAllConnectionsAreFaulty()
        {
            busInstance1.SetupCircuitBreakerHasTripped();
            busInstance2.SetupCircuitBreakerHasTripped();

            var startUtc = DateTime.UtcNow;
            await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(DateTime.UtcNow >= startUtc + settings.BackoffDelayIfAllConnectionsFaulty);
            Assert.IsTrue(DateTime.UtcNow <= startUtc + settings.BackoffDelayIfAllConnectionsFaulty + TimeSpan.FromSeconds(4));
        }

        [TestMethod]
        public async Task ReturnMessageId()
        {
            var results = await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(azureMessage.MessageId, results.First().Id);
        }

        [TestMethod]
        public async Task ReturnMessageBodyFromUtf8Json()
        {
            var results = await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual("message body", results.First().Body);
        }

        [TestMethod]
        public async Task ReturnMessageBodyTypeNamesFromHeader()
        {
            var results = await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual("System.String", results.First().MessageTypeNames.First());
        }

        [TestMethod]
        public async Task ReturnMessageDequeueTime()
        {
            var results = await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            var dequeueTime = results.First().DequeuedUtc;
            Assert.IsTrue(dequeueTime >= DateTime.UtcNow - TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public async Task ReturnLockExpiryTime()
        {
            var results = await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(azureMessage.SystemProperties.LockedUntilUtc, results.First().LockExpiresUtc);
        }

        [TestMethod]
        public async Task ReturnUserPropertiesAsCustomMessageHeaders()
        {
            var results = await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(azureMessage.UserProperties.First().Key, results.First().Headers.First().HeaderName);
            Assert.AreEqual(azureMessage.UserProperties.First().Value, results.First().Headers.First().Value);
        }

        [TestMethod]
        public async Task ReturnLockTokenInProviderData()
        {
            var results = await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(azureMessage.SystemProperties.LockToken, ((ProviderData)results.First().ProviderData).LockToken);
        }

        [TestMethod]
        public async Task ReturnMessageDequeueCount()
        {
            var results = await source.WaitForNextMessageBatch(32, CancellationToken.None).ConfigureAwait(false);
            var dequeueCount = results.First().DequeuedCount;
            Assert.AreEqual(azureMessage.SystemProperties.DeliveryCount, dequeueCount);
        }
    }
}
