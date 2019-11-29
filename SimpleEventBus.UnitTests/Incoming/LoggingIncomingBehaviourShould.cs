using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Incoming;
using System;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Incoming
{
    [TestClass]
    public class LoggingIncomingBehaviourShould
    {
        readonly Mock<ILogger<LoggingIncomingBehaviour>> mockLogger = new Mock<ILogger<LoggingIncomingBehaviour>>();
        LoggingIncomingBehaviour behaviour;
        bool nextActionWasCalled = false;

        [DataTestMethod]
        [DataRow(LogLevel.Warning)]
        [DataRow(LogLevel.Error)]
        public async Task LogMessageAtConfiguredLevel(LogLevel level)
        {
            behaviour = new LoggingIncomingBehaviour(mockLogger.Object, level);

            var message = IncomingMessageBuilder.BuildDefault();

            mockLogger.Setup(m => m.IsEnabled(level)).Returns(true);

            await behaviour.Process(message, new Context(null), NextAction).ConfigureAwait(false);

            mockLogger
                .Verify(
                    m => m.Log<object>(
                        level,
                        It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(),
                        It.IsAny<Func<object, Exception, string>>()),
                    Times.Once);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task CallNextAction(bool loggingEnabled)
        {
            behaviour = new LoggingIncomingBehaviour(mockLogger.Object, LogLevel.Warning);

            var message = IncomingMessageBuilder.BuildDefault();

            mockLogger.Setup(m => m.IsEnabled(LogLevel.Warning)).Returns(loggingEnabled);

            await behaviour.Process(message, new Context(null), NextAction).ConfigureAwait(false);

            Assert.IsTrue(nextActionWasCalled);
        }

        [TestMethod]
        public async Task NotLogWhenLoggingIsDisabled()
        {
            behaviour = new LoggingIncomingBehaviour(mockLogger.Object, LogLevel.Warning);

            var message = IncomingMessageBuilder.BuildDefault();

            mockLogger.Setup(m => m.IsEnabled(LogLevel.Warning)).Returns(false);

            await behaviour.Process(message, new Context(null), NextAction).ConfigureAwait(false);

            mockLogger
                .Verify(
                    m => m.Log<object>(
                        It.IsAny<LogLevel>(),
                        It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(),
                        It.IsAny<Func<object, Exception, string>>()),
                    Times.Never);
        }

        private Task NextAction(IncomingMessage message, Context context)
        {
            nextActionWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
