using Moq;
using SimpleEventBus.Abstractions.Incoming;

namespace SimpleEventBus.UnitTests.Incoming
{
    static class MockIncomingBehaviourExtensions
    {
        public static void VerifyProcessCalledOnce(this Mock<IIncomingBehaviour> mock, IncomingMessage message)
            => mock.Verify(m => m.Process(message, It.IsAny<Context>(), It.IsAny<IncomingPipelineAction>()), Times.Once);
    }
}
