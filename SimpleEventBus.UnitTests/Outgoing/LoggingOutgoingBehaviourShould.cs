using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Outgoing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Outgoing
{
    [TestClass]
    public class LoggingOutgoingBehaviourShould
    {
        LoggingOutgoingBehaviour behaviour;
        OutgoingMessage[] messages;
        bool nextActionWasCalled = false;
        readonly Mock<ILogger<LoggingOutgoingBehaviour>> mockLogger = new Mock<ILogger<LoggingOutgoingBehaviour>>();

        [TestInitialize]
        public void Setup()
        {
            behaviour = new LoggingOutgoingBehaviour(mockLogger.Object, LogLevel.Information);

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

        [DataTestMethod]
        public async Task LogMessageAtConfiguredLevel()
        {
            mockLogger.Setup(m => m.IsEnabled(LogLevel.Information)).Returns(true);

            await behaviour
                .Process(
                    messages,
                    NextAction)
                .ConfigureAwait(false);

            mockLogger
                .Verify(
                    m => m.Log<object>(
                        LogLevel.Information,
                        It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(),
                        It.IsAny<Func<object, Exception, string>>()),
                    Times.Once);
        }

        private Task NextAction(IEnumerable<OutgoingMessage> messages)
        {
            nextActionWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
