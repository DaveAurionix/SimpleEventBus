using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.AzureServiceBusTransport.Failover;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    [TestClass]
    public class AzureServiceBusMessageSinkShould
    {
        readonly Mock<IFailoverStrategy> mockFailoverStrategy = new Mock<IFailoverStrategy>();
        AzureServiceBusMessageSink sink;
        OutgoingMessage outgoingMessage;

        [TestInitialize]
        public void Setup()
        {
            sink = new AzureServiceBusMessageSink(mockFailoverStrategy.Object);
            outgoingMessage = new OutgoingMessage(
                Guid.NewGuid().ToString(),
                "body content",
                new[] { "Type name" });
        }

        [TestMethod]
        public async Task SendMessagesToTopicClient()
        {
            await sink
                .Sink(new[] { outgoingMessage })
                .ConfigureAwait(false);

            AssertWasSent(list => list.Count == 1);
        }

        [TestMethod]
        public async Task SendMessagesToTopicClientInBatchesOf100()
        {
            await sink
                .Sink(Enumerable.Repeat(outgoingMessage, 101))
                .ConfigureAwait(false);

            AssertWasSent(list => list.Count == 100);
            AssertWasSent(list => list.Count == 1);
        }

        [TestMethod]
        public async Task SerialiseMessageBodyToUtf8Json()
        {
            await sink
                .Sink(new[] { outgoingMessage })
                .ConfigureAwait(false);

            AssertWasSent(
                list =>
                    JsonConvert.DeserializeObject(
                        Encoding.UTF8.GetString(list.First().Body))
                    .ToString() == "body content");
        }

        [TestMethod]
        public async Task SetMessageContentTypeToApplicationJson()
        {
            await sink
                .Sink(new[] { outgoingMessage })
                .ConfigureAwait(false);

            AssertWasSent(list => list.First().ContentType == "application/json");
        }

        [TestMethod]
        public async Task SetMessageTypeNamesUserProperty()
        {
            await sink
                .Sink(new[] { outgoingMessage })
                .ConfigureAwait(false);

            AssertWasSent(list => list.First().UserProperties[TransportHeaders.MessageTypeNames].ToString() == ";Type name;");
        }

        [TestMethod]
        public async Task NotAddSpecificReceivingEndpointNameIfNotSet()
        {
            await sink
                .Sink(new[] { outgoingMessage })
                .ConfigureAwait(false);

            AssertWasSent(
                list => !list.First().UserProperties.ContainsKey(TransportHeaders.SpecificEndpoint));
        }

        [TestMethod]
        public async Task AddSpecificReceivingEndpointNameIfSet()
        {
            outgoingMessage = new OutgoingMessage(
                Guid.NewGuid().ToString(),
                "body content",
                new[] { "Type name" },
                specificReceivingEndpointName: "An.Endpoint");

            await sink
                .Sink(new[] { outgoingMessage })
                .ConfigureAwait(false);

            AssertWasSent(
                list => list.First().UserProperties[TransportHeaders.SpecificEndpoint].ToString() == "An.Endpoint");
        }

        [TestMethod]
        public async Task AddCustomHeaders()
        {
            outgoingMessage = new OutgoingMessage(
                Guid.NewGuid().ToString(),
                "body content",
                new[] { "Type name" },
                new[]
                {
                    new Header("Custom", "Header")
                });

            await sink
                .Sink(new[] { outgoingMessage })
                .ConfigureAwait(false);

            AssertWasSent(
                list => list.First().UserProperties["Custom"].ToString() == "Header");
        }

        private void AssertWasSent(Func<IList<Message>, bool> condition)
        {
            mockFailoverStrategy.Verify(
                m => m.Send(
                    It.Is<IList<Message>>(
                        list => condition(list))),
                Times.Once);
        }
    }
}
