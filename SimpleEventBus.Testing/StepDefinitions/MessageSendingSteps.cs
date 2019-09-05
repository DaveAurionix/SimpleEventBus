using GherkinSpec.TestModel;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Outgoing;
using System.Threading.Tasks;

namespace SimpleEventBus.Testing.StepDefinitions
{
    [Steps]
    public class MessageSendingSteps
    {
        private readonly IMessagePublisher messagePublisher;
        private readonly TestData testData;

        public MessageSendingSteps(IMessagePublisher messagePublisher, TestData testData)
        {
            this.messagePublisher = messagePublisher;
            this.testData = testData;
        }

        [When("an event is published")]
        public async Task WhenAnEventIsPublished()
        {
            await messagePublisher
                .PublishEvent(
                    new TestEvent
                    {
                        Property = testData.TestEventContent
                    },
                    new HeaderCollection
                    {
                        { "Correlation-ID", testData.CorrelationId }
                    })
                .ConfigureAwait(false);
        }

        [When("an event causing an exception is published")]
        public async Task WhenAnEventCausingAnExceptiobIsPublished()
        {
            await messagePublisher
                .PublishEvent(
                    new FailingTestEvent
                    {
                        Property = testData.TestEventContent
                    },
                    new HeaderCollection
                    {
                        { "Correlation-ID", testData.CorrelationId }
                    })
                .ConfigureAwait(false);
        }

        [When("an event is published without a Correlation-ID")]
        public async Task WhenAnEventIsPublishedWithoutCorrelationID()
        {
            await messagePublisher
                .PublishEvent(
                    new TestEvent
                    {
                        Property = testData.TestEventContent
                    })
                .ConfigureAwait(false);
        }

        [When("an event suitable for testing multiple handlers is published")]
        public async Task WhenAnEventForMultipleHandlersIsPublished()
        {
            await messagePublisher
                .PublishEvent(
                    new TestEventReceivedMultipleTimes
                    {
                        Property = testData.TestEventContent
                    },
                    new HeaderCollection
                    {
                        { "Correlation-ID", testData.CorrelationId }
                    })
                .ConfigureAwait(false);
        }

        [When("a command is sent")]
        public async Task WhenACommandIsSent()
        {
            await messagePublisher
                .SendCommand(
                    new TestCommand
                    {
                        Property = testData.TestCommandContent
                    },
                    new HeaderCollection
                    {
                        { "Correlation-ID", testData.CorrelationId }
                    })
                .ConfigureAwait(false);
        }
    }
}
