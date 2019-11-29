using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Incoming;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests
{
    [TestClass]
    public sealed class EndpointShould : IDisposable
    {
        private Endpoint endpoint;
        private readonly Mock<ILogger<Endpoint>> mockLogger = new Mock<ILogger<Endpoint>>();
        private readonly Mock<IIncomingPipeline> mockPipeline = new Mock<IIncomingPipeline>();
        private readonly Mock<IMessageSource> mockSource = new Mock<IMessageSource>();

        [TestInitialize]
        public void Setup()
        {
            endpoint = new Endpoint(
                mockSource.Object,
                mockPipeline.Object,
                mockLogger.Object,
                useConcurrentFetching: false,
                batchSizeProvider: new FixedBatchSize(10));
        }

        [TestCleanup]
        public void Dispose()
        {
            endpoint.Dispose();
        }

        [TestMethod]
        public async Task LogWhenListeningStarts()
        {
            await endpoint
                .StartListening()
                .ConfigureAwait(false);

            mockLogger.VerifyLogged(LogLevel.Information, "Messaging endpoint is initialising.");
            mockLogger.VerifyLogged(LogLevel.Information, "Messaging endpoint is listening for messages.");
        }

        [TestMethod]
        public async Task LogWhenListeningStops()
        {
            await endpoint
                .StartListening()
                .ConfigureAwait(false);

            await Task
                .Delay(TimeSpan.FromSeconds(1))
                .ConfigureAwait(false);

            await endpoint
                .ShutDown()
                .ConfigureAwait(false);

            mockLogger.VerifyLogged(LogLevel.Information, "Messaging endpoint has stopped listening for messages.");
        }

        [TestMethod]
        public async Task CloseMessageSourceWhenShuttingDown()
        {
            await endpoint
                .ShutDown()
                .ConfigureAwait(false);

            mockSource.Verify(m => m.Close(), Times.Once);
        }

        [TestMethod]
        public async Task WaitUntilCancellationWhenAsked()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(
                delay: TimeSpan.FromSeconds(3)))
            {
                var startedAt = DateTime.UtcNow;

                await endpoint
                    .StartWaitThenShutDown(cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                Assert.IsTrue(
                    DateTime.UtcNow >= startedAt + TimeSpan.FromSeconds(2.5));
            }
        }

        [TestMethod]
        public async Task ThrowExceptionIfStartedTwice()
        {
            await endpoint
                .StartListening()
                .ConfigureAwait(false);

            var exception = await Assert
                .ThrowsExceptionAsync<InvalidOperationException>(() => endpoint.StartListening())
                .ConfigureAwait(false);

            Assert.AreEqual("This endpoint has already been started.", exception.Message);
        }

        [TestMethod]
        public async Task NotThrowExceptionIfShutdownWithoutBeingStarted()
        {
            await endpoint
                .ShutDown()
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NotThrowExceptionIfShutDownTwice()
        {
            await endpoint
                .StartListening()
                .ConfigureAwait(false);

            await endpoint
                .ShutDown()
                .ConfigureAwait(false);

            await endpoint
                .ShutDown()
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task InitialisePipelineWhenListeningStarts()
        {
            await endpoint
                .StartListening()
                .ConfigureAwait(false);

            mockPipeline.Verify(
                m => m.Initialise(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task RetrieveMessagesFromSourceAndDispatchThemToPipelineWhenListening()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(
                delay: TimeSpan.FromSeconds(1)))
            {
                var startedAt = DateTime.UtcNow;

                await endpoint
                    .StartWaitThenShutDown(cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                mockSource.Verify(
                    m => m.WaitForNextMessageBatch(
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()),
                    Times.AtLeastOnce);

                mockPipeline.Verify(
                    m => m.Process(
                        It.IsAny<IEnumerable<IncomingMessage>>(),
                        It.IsAny<CancellationToken>()),
                    Times.AtLeastOnce);
            }
        }
    }
}
