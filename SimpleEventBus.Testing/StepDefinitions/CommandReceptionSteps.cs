using GherkinSpec.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace SimpleEventBus.Testing.StepDefinitions
{
    [Steps]
    public class CommandReceptionSteps
    {
        private readonly TestCommandHandler testCommandHandler;
        private readonly TestData testData;

        public CommandReceptionSteps(TestCommandHandler testCommandHandler, TestData testData)
        {
            this.testCommandHandler = testCommandHandler;
            this.testData = testData;
        }

        [Given("an endpoint has registered that it can handle commands")]
        public static void GivenAnEndpointHasRegisteredThatItCanHandleTestCommands()
        {
        }

        [Then("the endpoint receives the command")]
        [EventuallySucceeds]
        public void ThenTheTestCommandIsReceived()
        {
            Assert.AreEqual(
                1,
                testCommandHandler.ReceivedMessages.Count(
                    capturedMessages => capturedMessages.Message.Property == testData.TestCommandContent));
        }
    }
}
