using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.InMemoryTransport.UnitTests
{
    [TestClass]
    public class InMemoryBusConnectionShould
    {
        const string MessageType = "System.String";
        private OutgoingMessage message;
        private OutgoingMessage message2;
        private InMemoryBus bus;

        [TestInitialize]
        public void Setup()
        {
            message = new OutgoingMessage(Guid.NewGuid().ToString(), "Hello world", new[] { MessageType });
            message2 = new OutgoingMessage(Guid.NewGuid().ToString(), "Hello world 2", new[] { MessageType });
            bus = new InMemoryBus();
        }

        [TestMethod]
        public async Task ReceiveMessagesFromOwnConnectionWhenSubscribed()
        {
            using var connection = new InMemoryBusConnection(bus);
            await connection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await connection
                .Sink(new[] { message })
                .ConfigureAwait(false);

            var messages = await connection
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, messages.Count());
            Assert.AreEqual(message.Id, messages.First().Id);
        }

        [TestMethod]
        public async Task NotReceiveMessagesFromOwnConnectionWhenNotSubscribed()
        {
            using var connection = new InMemoryBusConnection(bus);
            await connection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { "Another.Type" }), CancellationToken.None)
                .ConfigureAwait(false);

            await connection
                .Sink(new[] { message })
                .ConfigureAwait(false);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var messages = await connection
                .WaitForNextMessageBatch(1, cancellationTokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(0, messages.Count());
        }

        [TestMethod]
        public async Task ReceiveMessagesFromOtherConnectionsWhenSubscribed()
        {
            using var ownConnection = new InMemoryBusConnection(bus);
            using var otherConnection = new InMemoryBusConnection(bus);

            await ownConnection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await otherConnection
                .Sink(new[] { message })
                .ConfigureAwait(false);

            var messages = await ownConnection
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, messages.Count());
            Assert.AreEqual(message.Id, messages.First().Id);
        }

        [TestMethod]
        public async Task NotReceiveMessagesFromOtherConnectionsWhenNotSubscribed()
        {
            using var ownConnection = new InMemoryBusConnection(bus);
            using var otherConnection = new InMemoryBusConnection(bus);

            await ownConnection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { "Another.Type" }), CancellationToken.None)
                .ConfigureAwait(false);

            await otherConnection
                .Sink(new[] { message })
                .ConfigureAwait(false);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var messages = await ownConnection
                .WaitForNextMessageBatch(1, cancellationTokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(0, messages.Count());
        }

        [TestMethod]
        public async Task NotReceiveMessagesFromOtherConnectionsWhenSubscribedButTargetEndpointNameIsNotMe()
        {
            using var ownConnection = new InMemoryBusConnection(bus);
            using var otherConnection = new InMemoryBusConnection(bus);

            await ownConnection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await otherConnection
                .Sink(
                    new[] {
                                new OutgoingMessage(
                                    id: Guid.NewGuid().ToString(),
                                    body: "hello",
                                    messageTypeNames: new[] { MessageType },
                                    specificReceivingEndpointName: "not me")
                    })
                .ConfigureAwait(false);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var messages = await ownConnection
                .WaitForNextMessageBatch(1, cancellationTokenSource.Token)
                .ConfigureAwait(false);

            Assert.AreEqual(0, messages.Count());
        }

        [TestMethod]
        public async Task ReceiveMessagesFromOtherConnectionsWhenNotSubscribedButTargetEndpointNameIsMe()
        {
            using var ownConnection = new InMemoryBusConnection(bus);
            using var otherConnection = new InMemoryBusConnection(bus);

            await ownConnection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { "Another.Type" }), CancellationToken.None)
                .ConfigureAwait(false);

            var message = new OutgoingMessage(
                id: Guid.NewGuid().ToString(),
                body: "hello",
                messageTypeNames: new[] { MessageType },
                specificReceivingEndpointName: "TestEndpoint");

            await otherConnection
                .Sink(new[] { message })
                .ConfigureAwait(false);

            var messages = await ownConnection
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, messages.Count());
            Assert.AreEqual(message.Id, messages.First().Id);
        }

        [TestMethod]
        public async Task HideMessagesThatHaveBeenDequeued()
        {
            using var connection = new InMemoryBusConnection(bus)
            {
                LockTime = TimeSpan.FromSeconds(1)
            };

            await connection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await connection
                .Sink(new[] { message, message2 })
                .ConfigureAwait(false);

            var messages = await connection
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message.Id, messages.Single().Id);

            messages = await connection
                .WaitForNextMessageBatch(2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message2.Id, messages.Single().Id);
        }

        [TestMethod]
        public async Task ReshowMessagesAfterLockTimeHasExpiredThatAreNotCompleted()
        {
            using var connection = new InMemoryBusConnection(bus)
            {
                LockTime = TimeSpan.FromSeconds(1)
            };

            await connection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await connection
                .Sink(new[] { message, message2 })
                .ConfigureAwait(false);

            var messages = await connection
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message.Id, messages.Single().Id);

            await Task
                .Delay(TimeSpan.FromSeconds(2))
                .ConfigureAwait(false);

            messages = await connection
                .WaitForNextMessageBatch(2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message.Id, messages.Last().Id);
        }

        [TestMethod]
        public async Task ReshowAbandonedMessagesImmediately()
        {
            using var connection = new InMemoryBusConnection(bus)
            {
                LockTime = TimeSpan.FromSeconds(10)
            };

            await connection
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await connection
                .Sink(new[] { message, message2 })
                .ConfigureAwait(false);

            var messages = await connection
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            var incomingMessage = messages.Single();
            Assert.AreEqual(message.Id, incomingMessage.Id);

            await connection
                .Abandon(incomingMessage)
                .ConfigureAwait(false);

            messages = await connection
                .WaitForNextMessageBatch(2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message.Id, messages.Last().Id);
        }
    }
}
