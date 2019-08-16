using Microsoft.Extensions.Logging;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.Incoming
{
    public class CorrelationIdIncomingBehaviour : IIncomingBehaviour, IOutgoingHeaderProvider
    {
        private static readonly AsyncLocal<string> incomingCorrelationIdHeaderValue = new AsyncLocal<string>();
        private readonly ILogger<CorrelationIdIncomingBehaviour> logger;

        public CorrelationIdIncomingBehaviour(ILogger<CorrelationIdIncomingBehaviour> logger)
        {
            this.logger = logger;
        }

        public async Task Process(IncomingMessage message, Context context, IncomingPipelineAction next)
        {
            var correlationId = message.Headers.GetValueOrDefault(SharedConstants.CorrelationIdHeaderName);

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            incomingCorrelationIdHeaderValue.Value = correlationId;

            using (logger.BeginScope("Handling message with correlation id {CorrelationId}", correlationId))
            {
                await next(message, context)
                    .ConfigureAwait(false);
            }
        }

        public IEnumerable<Header> GetOutgoingHeaders()
            => new[] { new Header(SharedConstants.CorrelationIdHeaderName, incomingCorrelationIdHeaderValue.Value) };
    }
}
