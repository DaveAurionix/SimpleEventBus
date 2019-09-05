using GherkinSpec.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace SimpleEventBus.Testing.StepDefinitions
{
    [Steps]
    public class EventSubscriptionSteps
    {
        private readonly TestEventHandler testEventHandler;
        private readonly FailingTestEventHandler failingTestEventHandler;
        private readonly TestData testData;
        private readonly RetryOptions retryOptions;
        private readonly TestFirstEventReceivedMultipleTimesHandler firstHandler;
        private readonly TestSecondEventReceivedMultipleTimesHandler secondHandler;

        public EventSubscriptionSteps(TestEventHandler testEventHandler, FailingTestEventHandler failingTestEventHandler, TestData testData, RetryOptions retryOptions, TestFirstEventReceivedMultipleTimesHandler firstHandler, TestSecondEventReceivedMultipleTimesHandler secondHandler)
        {
            this.testEventHandler = testEventHandler;
            this.failingTestEventHandler = failingTestEventHandler;
            this.testData = testData;
            this.retryOptions = retryOptions;
            this.firstHandler = firstHandler;
            this.secondHandler = secondHandler;
        }

        [Given("an endpoint has subscribed to events")]
        [Given("an endpoint has subscribed to events causing an exception")]
        [Given("an endpoint has subscribed to the same event multiple times")]
        public static void GivenAnEventSubscriptionIsSetUp()
        {
        }

        [Then("the endpoint receives the event")]
        [EventuallySucceeds]
        public void ThenTheEventIsReceived()
        {
            Assert.AreEqual(
                1,
                testEventHandler.ReceivedMessages.Count(
                    capturedMessage => capturedMessage.Message.Property == testData.TestEventContent));
        }

        [Then("the endpoint receives the event with the correct Correlation-ID")]
        [EventuallySucceeds]
        public void ThenTheEventIsReceivedWithExactCorrelationId()
        {
            Assert.AreEqual(
                1,
                testEventHandler.ReceivedMessages.Count(
                    capturedMessage => capturedMessage.CorrelationId == testData.CorrelationId));
        }

        [Then("the endpoint receives the event with any Correlation-ID")]
        [EventuallySucceeds]
        public void ThenTheEventIsReceivedWithAnyCorrelationId()
        {
            Assert.AreEqual(
                1,
                testEventHandler.ReceivedMessages.Count(
                    capturedMessage => !string.IsNullOrWhiteSpace(capturedMessage.CorrelationId)
                        && capturedMessage.Message.Property == testData.TestEventContent));
        }

        [Then("the endpoint receives the event several times immediately according to the retry settings")]
        [EventuallySucceeds]
        public void ThenTheEventIsReceivedImmediatelyAccordingToRetrySettings()
        {
            var firstReceivedAtUtc = failingTestEventHandler
                .ReceivedMessages
                .Where(capturedMessage => capturedMessage.CorrelationId == testData.CorrelationId)
                .OrderBy(message => message.CapturedAtUtc)
                .First()
                .CapturedAtUtc;

            Assert.AreEqual(
                retryOptions.MaximumImmediateAttempts,
                failingTestEventHandler
                    .ReceivedMessages
                    .Count(
                        capturedMessage => capturedMessage.CorrelationId == testData.CorrelationId
                        && (capturedMessage.CapturedAtUtc - firstReceivedAtUtc) < retryOptions.DeferredRetryInterval - TimeSpan.FromSeconds(1)));
        }

        [Then("the endpoint receives the event several times eventually according to the retry settings")]
        [EventuallySucceeds]
        public void ThenTheEventIsReceivedEventuallyAccordingToRetrySettings()
        {
            var expectedDelaySeconds = retryOptions.DeferredRetryInterval.TotalSeconds * retryOptions.MaximumDeferredAttempts +
              10;

            var firstReceivedAtUtc = failingTestEventHandler
                .ReceivedMessages
                .Where(capturedMessage => capturedMessage.CorrelationId == testData.CorrelationId)
                .OrderBy(message => message.CapturedAtUtc)
                .First()
                .CapturedAtUtc;

            Assert.AreEqual(
                retryOptions.MaximumImmediateAttempts + retryOptions.MaximumDeferredAttempts,
                failingTestEventHandler
                    .ReceivedMessages
                    .Count(
                        capturedMessage => capturedMessage.CorrelationId == testData.CorrelationId
                        && (capturedMessage.CapturedAtUtc - firstReceivedAtUtc) <= TimeSpan.FromSeconds(expectedDelaySeconds)));
        }

        [Then("all handlers receive the event")]
        [EventuallySucceeds]
        public void ThenAllHandlersReceiveTheEvent()
        {
            Assert.AreEqual(
                1,
                firstHandler.ReceivedMessages.Count(
                    capturedMessage => capturedMessage.Message.Property == testData.TestEventContent));

            Assert.AreEqual(
                1,
                secondHandler.ReceivedMessages.Count(
                    capturedMessage => capturedMessage.Message.Property == testData.TestEventContent));
        }

        [Then("all handlers receive the event with the same Correlation-ID")]
        [EventuallySucceeds]
        public void ThenAllHandlersReceiveTheEventWithSameCorrelationID()
        {
            Assert.AreEqual(
                1,
                firstHandler.ReceivedMessages.Count(
                    capturedMessage => capturedMessage.CorrelationId == testData.CorrelationId));

            Assert.AreEqual(
                1,
                secondHandler.ReceivedMessages.Count(
                    capturedMessage => capturedMessage.CorrelationId == testData.CorrelationId));
        }
    }
}
