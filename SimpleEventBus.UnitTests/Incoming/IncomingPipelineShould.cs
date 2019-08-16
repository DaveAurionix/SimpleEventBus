using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Incoming;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Incoming
{
    [TestClass]
    public sealed class IncomingPipelineShould : IDisposable
    {
        private IncomingPipeline pipeline;
        private readonly Mock<IIncomingBehaviour> mockBehaviour = new Mock<IIncomingBehaviour>();
        private readonly Mock<IServiceScopeFactory> mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        private readonly Mock<IHandlerInvoker> mockHandlerInvoker = new Mock<IHandlerInvoker>();
        private readonly Mock<IMessageSource> mockMessageSource = new Mock<IMessageSource>();
        private readonly RetryOptions retryOptions = new RetryOptions();
        private IncomingMessage messageCausingException;
        private IncomingMessage normalMessage;
        private CancellationTokenSource cancellationTokenSource;

        [TestInitialize]
        public async Task Setup()
        {
            cancellationTokenSource = new CancellationTokenSource();

            pipeline = new IncomingPipeline(
                mockMessageSource.Object,
                new[] { mockBehaviour.Object },
                mockServiceScopeFactory.Object,
                mockHandlerInvoker.Object,
                NullLogger<IncomingPipeline>.Instance,
                retryOptions);

            mockBehaviour
                .Setup(
                    m => m.Process(
                        It.Is<IncomingMessage>(message => ((string)message.Body) == "throw this"),
                        It.IsAny<Context>(),
                        It.IsAny<IncomingPipelineAction>()))
                .Throws(new InvalidOperationException());

            messageCausingException = IncomingMessageBuilder.BuildWithBody("throw this");
            normalMessage = IncomingMessageBuilder.BuildDefault();

            await pipeline
                .Initialise(CancellationToken.None)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
        }

        [TestMethod]
        public void InitialiseHandlerInvokerWhenAsked()
        {
            mockHandlerInvoker.Verify(
                m => m.Initialise(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessMessages()
        {
            await ProcessOneNormalMessage().ConfigureAwait(false);
            mockBehaviour.VerifyProcessCalledOnce(normalMessage);
        }

        [TestMethod]
        public async Task CompleteMessagesAfterSuccessfulProcessing()
        {
            await ProcessOneExceptionMessageAndOneNormalMessage().ConfigureAwait(false);
            mockMessageSource.VerifyCompleteCalledNever(messageCausingException);
            mockMessageSource.VerifyCompleteCalledOnce(normalMessage);
        }

        [TestMethod]
        public async Task StillProcessOtherMessagesInBatchWhenOneMessageResultsInAnException()
        {
            await ProcessOneExceptionMessageAndOneNormalMessage().ConfigureAwait(false);
            mockBehaviour.VerifyProcessCalledOnce(messageCausingException);
            mockBehaviour.VerifyProcessCalledOnce(normalMessage);
        }

        [TestMethod]
        public async Task AbandonMessageIfHandlerThrowsExceptionOnFirstAttemptToProcessMessage()
        {
            await ProcessOneExceptionMessageAndOneNormalMessage().ConfigureAwait(false);
            mockMessageSource.VerifyAbandonCalledOnce(messageCausingException);
            mockMessageSource.VerifyAbandonCalledNever(normalMessage);
        }

        [TestMethod]
        public async Task DeferMessageIfHandlerThrowsExceptionMoreThanConfiguredImmediateRetriesCount()
        {
            var message = IncomingMessageBuilder.New()
                .WithBody("throw this")
                .WithDequeuedCount(retryOptions.MaximumImmediateAttempts)
                .Build();

            await Process(message).ConfigureAwait(false);
            mockMessageSource.VerifyDeferCalledOnce(message, "Exception handling message");
            mockMessageSource.VerifyAbandonCalledNever(message);
            mockMessageSource.VerifyDeadLetterCalledNever(message);
        }

        [TestMethod]
        public async Task DeadLetterMessageIfHandlerThrowsExceptionMoreThanConfiguredDeferredRetriesCount()
        {
            var message = IncomingMessageBuilder.New()
                .WithBody("throw this")
                .WithDequeuedCount(retryOptions.MaximumImmediateAttempts + retryOptions.MaximumDeferredAttempts)
                .Build();

            await Process(message).ConfigureAwait(false);
            mockMessageSource.VerifyDeadLetterCalledOnce(message, "Exception handling message");
            mockMessageSource.VerifyAbandonCalledNever(message);
            mockMessageSource.VerifyDeferCalledNever(message);
        }

        [TestMethod]
        public async Task AbandonAllRemainingMessagesIfProcessingIsCancelled()
        {
            cancellationTokenSource.Cancel();
            await ProcessOneNormalMessage().ConfigureAwait(false);
            mockMessageSource.VerifyAbandonCalledOnce(normalMessage);
        }

        private Task ProcessOneExceptionMessageAndOneNormalMessage()
            => pipeline
                .Process(
                    new[] {
                        messageCausingException,
                        normalMessage
                    },
                    cancellationTokenSource.Token);

        private Task ProcessOneNormalMessage()
            => Process(normalMessage);

        private Task Process(IncomingMessage message)
            => pipeline
                .Process(
                    new[] {
                        message
                    },
                    cancellationTokenSource.Token);
    }
}
