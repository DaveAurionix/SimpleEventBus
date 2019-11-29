using Microsoft.Extensions.Logging;
using SimpleEventBus.Abstractions.Outgoing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventBus.Outgoing
{
    class LoggingOutgoingBehaviour : IOutgoingBehaviour
    {
        private readonly ILogger<LoggingOutgoingBehaviour> logger;
        private readonly LogLevel logLevel;

        public LoggingOutgoingBehaviour(ILogger<LoggingOutgoingBehaviour> logger, LogLevel logLevel)
        {
            this.logger = logger;
            this.logLevel = logLevel;
        }

        public Task Process(IEnumerable<OutgoingMessage> messages, OutgoingPipelineAction nextAction)
        {
            if (logger.IsEnabled(logLevel))
            {
                foreach (var message in messages)
                {
                    var correlationId = message.Headers.GetValueOrDefault(SharedConstants.CorrelationIdHeaderName);
                    var typeName = message.MessageTypeNames.First();

                    logger.Log(
                        logLevel,
                        $"SENDING {typeName} (id={message.Id}; correlationId={correlationId})");
                }
            }

            return nextAction(messages);
        }
    }
}
