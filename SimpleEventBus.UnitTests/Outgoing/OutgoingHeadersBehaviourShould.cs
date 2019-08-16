using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Outgoing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Outgoing
{
    [TestClass]
    public class OutgoingHeadersBehaviourShould
    {
        class Provider : IOutgoingHeaderProvider
        {
            public IEnumerable<Header> GetOutgoingHeaders()
            {
                return new[]
                {
                    new Header("Header Name", "Header Value")
                };
            }
        }

        OutgoingHeadersBehaviour behaviour;
        OutgoingMessage[] messages;
        bool nextActionWasCalled = false;

        [TestInitialize]
        public void Setup()
        {
            behaviour = new OutgoingHeadersBehaviour(new OutgoingHeaderProviders(new[] { new Provider() }));

            messages = new[]
            {
                new OutgoingMessage(Guid.NewGuid().ToString(), null, new[] { "test" })
            };
        }

        [TestMethod]
        public async Task InvokeTheNextBehaviourInTheChain()
        {
            await behaviour
                .Process(
                    messages,
                    NextAction)
                .ConfigureAwait(false);

            Assert.IsTrue(nextActionWasCalled);
        }

        [TestMethod]
        public async Task AttachHeadersFromOutgoingHeadersProviders()
        {
            await behaviour
                .Process(
                    messages,
                    NextAction)
                .ConfigureAwait(false);

            Assert.AreEqual("Header Value", messages[0].Headers.GetValueOrDefault("Header Name"));
        }

        [TestMethod]
        public async Task NotOverwriteHeadersAlreadyOnTheMessage()
        {
            messages[0].Headers.Add("Header Name", "Original Header Value");

            await behaviour
                .Process(
                    messages,
                    NextAction)
                .ConfigureAwait(false);

            Assert.AreEqual("Original Header Value", messages[0].Headers.GetValueOrDefault("Header Name"));
        }

        private Task NextAction(IEnumerable<OutgoingMessage> messages)
        {
            nextActionWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
