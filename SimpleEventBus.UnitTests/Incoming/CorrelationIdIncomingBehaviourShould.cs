using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Incoming;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Incoming
{
    [TestClass]
    public class CorrelationIdIncomingBehaviourShould
    {
        readonly CorrelationIdIncomingBehaviour behaviour = new CorrelationIdIncomingBehaviour(NullLogger<CorrelationIdIncomingBehaviour>.Instance);
        bool nextActionWasCalled = false;
        Header[] capturedHeaders;

        [TestMethod]
        public async Task InvokeTheNextBehaviourInTheChain()
        {
            await behaviour
                .Process(
                    IncomingMessageBuilder.BuildDefault(),
                    new Context(null),
                    NextAction)
                .ConfigureAwait(false);

            Assert.IsTrue(nextActionWasCalled);
        }

        [TestMethod]
        public async Task ReadCorrelationIdFromIncomingMessageHeader()
        {
            var message = IncomingMessageBuilder
                .New()
                .WithHeader(SharedConstants.CorrelationIdHeaderName, "expected id")
                .Build();

            await behaviour.Process(message, new Context(null), NextAction).ConfigureAwait(false);

            Assert.AreEqual(1, capturedHeaders.Length);
            Assert.AreEqual(SharedConstants.CorrelationIdHeaderName, capturedHeaders.First().HeaderName);
            Assert.AreEqual("expected id", capturedHeaders.First().Value);
        }

        [TestMethod]
        public async Task GenerateNewCorrelationIdIfIncomingMessageDoesNotIncludeOne()
        {
            var message = IncomingMessageBuilder
                .BuildDefault();

            await behaviour.Process(message, new Context(null), NextAction).ConfigureAwait(false);

            Assert.AreEqual(1, capturedHeaders.Length);
            Assert.AreEqual(SharedConstants.CorrelationIdHeaderName, capturedHeaders.First().HeaderName);
            Assert.IsTrue(Guid.TryParse(capturedHeaders.First().Value, out var generatedId));
            Assert.AreNotEqual(Guid.Empty, generatedId);
        }

        private Task NextAction(IncomingMessage message, Context context)
        {
            nextActionWasCalled = true;
            capturedHeaders = behaviour.GetOutgoingHeaders().ToArray();
            return Task.CompletedTask;
        }
    }
}
