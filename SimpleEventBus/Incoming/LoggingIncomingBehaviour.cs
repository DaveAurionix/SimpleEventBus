using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleEventBus.Abstractions.Incoming;

namespace SimpleEventBus.Incoming
{
    class LoggingIncomingBehaviour : IIncomingBehaviour
    {
        private readonly ILogger<LoggingIncomingBehaviour> logger;
        private readonly LogLevel logLevel;

        public LoggingIncomingBehaviour(ILogger<LoggingIncomingBehaviour> logger, LogLevel logLevel)
        {
            this.logger = logger;
            this.logLevel = logLevel;
        }

        public Task Process(IncomingMessage message, Context context, IncomingPipelineAction nextAction)
        {
            if (!logger.IsEnabled(logLevel))
            {
                return nextAction(message, context);
            }

            return ProcessCore(message, context, nextAction);
        }

        private async Task ProcessCore(IncomingMessage message, Context context, IncomingPipelineAction nextAction)
        {
            var correlationId = message.Headers.GetValueOrDefault(SharedConstants.CorrelationIdHeaderName);
            var typeName = message.MessageTypeNames.First();

            logger.Log(
                logLevel,
                $"HANDLING {typeName} (id={message.Id}; correlationId={correlationId}; attempt={message.DequeuedCount}; remainingLockTime={message.RemainingLockTime})");

            try
            {
                await nextAction(message, context)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    $"Error when handling {typeName} (id={message.Id}): {exception.Message}");
                throw;
            }
        }
    }
}
