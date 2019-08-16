using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Outgoing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Outgoing
{
    [TestClass]
    public class OutgoingPipelineShould
    {
        private readonly Mock<IOutgoingBehaviour> mockBehaviour = new Mock<IOutgoingBehaviour>();
        private readonly Mock<IMessageSink> mockSink = new Mock<IMessageSink>();

        private OutgoingPipeline pipeline;
        private OutgoingMessage[] messages;

        [TestInitialize]
        public void Setup()
        {
            pipeline = new OutgoingPipeline(
                new[] { mockBehaviour.Object },
                mockSink.Object,
                NullLogger<OutgoingPipeline>.Instance);

            messages = new[]
            {
                new OutgoingMessage(Guid.NewGuid().ToString(), null, new[] { "test" })
            };
        }

        [TestMethod]
        public async Task PassMessagesToTheFirstPipelineBehaviour()
        {
            await pipeline
                .Process(messages)
                .ConfigureAwait(false);

            mockBehaviour.Verify(
                m => m.Process(
                    messages,
                    It.IsAny<OutgoingPipelineAction>()),
                Times.Once);
        }

        [TestMethod]
        public async Task PassSinkAsLastAction()
        {
            OutgoingPipelineAction capturedNextAction = null;

            mockBehaviour
                .Setup(m => m.Process(It.IsAny<IEnumerable<OutgoingMessage>>(), It.IsAny<OutgoingPipelineAction>()))
                .Returns(Task.CompletedTask)
                .Callback<IEnumerable<OutgoingMessage>, OutgoingPipelineAction>(
                    (capturedMessages, nextAction) =>
                    {
                        capturedNextAction = nextAction;
                    });

            await pipeline
                .Process(messages)
                .ConfigureAwait(false);

            await capturedNextAction(messages)
                .ConfigureAwait(false);

            mockSink.Verify(m => m.Sink(messages), Times.Once);
        }
    }
}
