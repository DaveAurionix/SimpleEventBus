using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.FileTransport.UnitTests
{
    [TestClass]
    public sealed class FileBusConnectionShould : IDisposable
    {
        const string MessageType = "System.String";
        private OutgoingMessage message;
        private OutgoingMessage message2;
        private FileBusConnection bus;
        
        [TestInitialize]
        public void Setup()
        {
            var busPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "messages");
            message = new OutgoingMessage(Guid.NewGuid().ToString(), "Hello world", new[] { MessageType });
            message2 = new OutgoingMessage(Guid.NewGuid().ToString(), "Hello world 2", new[] { MessageType });
            bus = new FileBusConnection(
                busPath,
                new FullNameTypeMap());
        }

        [TestCleanup]
        public void Dispose()
        {
            bus.Dispose();
        }

        [TestMethod]
        public async Task ReceiveMessagesWhenSubscribed()
        {
            await bus
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await bus
                .Sink(new[] { message })
                .ConfigureAwait(false);

            var messages = await bus
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, messages.Count());
            Assert.AreEqual(message.Id, messages.First().Id);
        }

        [TestMethod]
        public async Task NotReceiveMessagesWhenNotSubscribed()
        {
            await bus
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { "Another.Type" }), CancellationToken.None)
                .ConfigureAwait(false);

            await bus
                .Sink(new[] { message })
                .ConfigureAwait(false);

            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var messages = await bus
                    .WaitForNextMessageBatch(1, cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                Assert.AreEqual(0, messages.Count());
            }
        }

        [TestMethod]
        public async Task NotReceiveMessagesWhenSubscribedButTargetEndpointNameIsNotMe()
        {
            await bus
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await bus
                .Sink(
                    new[] {
                        new OutgoingMessage(
                            id: Guid.NewGuid().ToString(),
                            body: "hello",
                            messageTypeNames: new[] { MessageType },
                            specificReceivingEndpointName: "not me")
                    })
                .ConfigureAwait(false);

            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var messages = await bus
                    .WaitForNextMessageBatch(1, cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                Assert.AreEqual(0, messages.Count());
            }
        }

        [TestMethod]
        public async Task ReceiveMessagesWhenNotSubscribedButTargetEndpointNameIsMe()
        {
            await bus
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { "Another.Type" }), CancellationToken.None)
                .ConfigureAwait(false);

            var message = new OutgoingMessage(
                id: Guid.NewGuid().ToString(),
                body: "hello",
                messageTypeNames: new[] { MessageType },
                specificReceivingEndpointName: "TestEndpoint");

            await bus
                .Sink(new[] { message })
                .ConfigureAwait(false);

            var messages = await bus
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, messages.Count());
            Assert.AreEqual(message.Id, messages.First().Id);
        }

        [TestMethod]
        public async Task HideMessagesThatHaveBeenDequeued()
        {
            bus.LockTime = TimeSpan.FromSeconds(1);

            await bus
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await bus
                .Sink(new[] { message, message2 })
                .ConfigureAwait(false);

            var messages = await bus
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message.Id, messages.Single().Id);

            messages = await bus
                .WaitForNextMessageBatch(2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message2.Id, messages.Single().Id);
        }

        [TestMethod]
        public async Task ReshowMessagesAfterLockTimeHasExpiredThatAreNotCompleted()
        {
            bus.LockTime = TimeSpan.FromSeconds(1);

            await bus
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await bus
                .Sink(new[] { message, message2 })
                .ConfigureAwait(false);

            var messages = await bus
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message.Id, messages.Single().Id);

            await Task
                .Delay(TimeSpan.FromSeconds(2))
                .ConfigureAwait(false);

            messages = await bus
                .WaitForNextMessageBatch(2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message.Id, messages.Last().Id);
        }

        [TestMethod]
        public async Task ReshowAbandonedMessagesImmediately()
        {
            bus.LockTime = TimeSpan.FromSeconds(10);

            await bus
                .EnsureSubscribed(new SubscriptionDescription("TestEndpoint", new[] { MessageType }), CancellationToken.None)
                .ConfigureAwait(false);

            await bus
                .Sink(new[] { message, message2 })
                .ConfigureAwait(false);

            var messages = await bus
                .WaitForNextMessageBatch(1, CancellationToken.None)
                .ConfigureAwait(false);

            var incomingMessage = messages.Single();
            Assert.AreEqual(message.Id, incomingMessage.Id);

            await bus
                .Abandon(incomingMessage)
                .ConfigureAwait(false);

            messages = await bus
                .WaitForNextMessageBatch(2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(message.Id, messages.Last().Id);
        }
    }
}
