using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Outgoing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Outgoing
{
    [TestClass]
    public class MessagePublisherShould
    {
        private readonly Mock<IOutgoingPipeline> mockPipeline = new Mock<IOutgoingPipeline>();

        [TestMethod]
        public async Task PublishEventThroughPipeline()
        {
            var publisher = new MessagePublisher(
                mockPipeline.Object,
                FullNameTypeMap.Instance,
                new Collection<CommandRoute>());

            var @event = new ExampleEvent { Property = "Hello world" };
            await publisher
                .PublishEvent(@event)
                .ConfigureAwait(false);

            mockPipeline.Verify(
                m => m.Process(
                    It.Is<IEnumerable<OutgoingMessage>>(
                        value => value.Single().Body == @event
                            && value.Single().MessageTypeNames.First() == FullNameTypeMap.Instance.GetNameForType(typeof(ExampleEvent))
                            && value.Single().SpecificReceivingEndpointName == null)),
                Times.Once);
        }

        [TestMethod]
        public async Task ThrowExceptionIfNoRouteConfiguredForCommand()
        {
            var publisher = new MessagePublisher(
                mockPipeline.Object,
                FullNameTypeMap.Instance,
                new Collection<CommandRoute>());

            var command = new ExampleCommand { Property = "Hello world" };
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => publisher
                    .SendCommand(command))
                    .ConfigureAwait(false);

            Assert.AreEqual(
                "No target endpoint has been configured for the command type \"SimpleEventBus.UnitTests.ExampleCommand, SimpleEventBus.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\".",
                exception.Message);
        }

        [TestMethod]
        public async Task SendCommandToTransport()
        {
            const string TargetEndpoint = "Target";

            var routes = new Collection<CommandRoute>
            {
                new CommandRoute(typeof(ExampleCommand), TargetEndpoint)
            };

            var publisher = new MessagePublisher(
                mockPipeline.Object,
                FullNameTypeMap.Instance,
                routes);

            var command = new ExampleCommand { Property = "Hello world" };
            await publisher
                .SendCommand(command)
                .ConfigureAwait(false);

            mockPipeline.Verify(
                m => m.Process(
                    It.Is<IEnumerable<OutgoingMessage>>(
                        value => value.Single().Body == command
                            && value.Single().MessageTypeNames.First() == FullNameTypeMap.Instance.GetNameForType(typeof(ExampleCommand))
                            && value.Single().SpecificReceivingEndpointName == TargetEndpoint)),
                Times.Once);
        }
    }
}
