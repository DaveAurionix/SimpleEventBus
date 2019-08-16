using GherkinSpec.TestModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.AzureServiceBusTransport.IntegrationTests.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.IntegrationTests.StepDefinitions
{
    [Steps]
    public sealed class FailoverSteps
    {
        private TestFailoverEventHandler testEventHandler;
        private Endpoint endpoint;
        private IMessagePublisher messagePublisher;
        private readonly AzureServiceBusTransportSettings transportSettings;
        private Guid uniqueId = Guid.NewGuid();

        public FailoverSteps(Settings settings)
        {
            transportSettings = settings.AzureServiceBusTransport.Clone();

            transportSettings.ConnectionStrings = new[]
            {
                "Endpoint=sb://badconnstring.somesuch.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=6SpfTD7REZVLBcgBjQ4epN8AOZAMbZTkE20Rq+/ACg8=",
                transportSettings.ConnectionStrings.Single()
            };
        }

        [Given("an endpoint has subscribed to events for a failover test")]
        public async Task GivenAnEventSubscriptionIsSetUp()
        {
            var serviceProvider = new ServiceCollection()
                .AddSimpleEventBus(
                    options => options
                        .UseEndpointName("SimpleEventBus.AzureServiceBusTransport.Fail.Tests")
                        .UseAzureServiceBus(transportSettings)
                        .Use(FailoverStrategy.ActivePassive)
                        .UseSingletonHandlersIn(typeof(FailoverSteps).Assembly))
                .BuildServiceProvider();

            endpoint = serviceProvider.GetRequiredService<Endpoint>();
            messagePublisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            testEventHandler = serviceProvider.GetRequiredService<TestFailoverEventHandler>();
            await endpoint
                .StartListening()
                .ConfigureAwait(false);
        }

        [When("a failover test event is published")]
        public Task WhenAnEventIsPublished()
            => messagePublisher
                .PublishEvent(
                    new TestFailoverEvent
                    {
                        Property = uniqueId.ToString()
                    });

        [Then("the endpoint receives the failover test event")]
        [EventuallySucceeds]
        public void ThenTheEventIsReceived()
        {
            Assert.AreEqual(
                1,
                testEventHandler.ReceivedMessages.Count(
                    capturedMessage => capturedMessage.Message.Property == uniqueId.ToString()));
        }
    }
}
