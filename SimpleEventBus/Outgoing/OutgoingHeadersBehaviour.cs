using SimpleEventBus.Abstractions.Outgoing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.Outgoing
{
    class OutgoingHeadersBehaviour : IOutgoingBehaviour
    {
        private readonly OutgoingHeaderProviders providers;

        public OutgoingHeadersBehaviour(OutgoingHeaderProviders providers)
        {
            this.providers = providers;
        }

        public async Task Process(IEnumerable<OutgoingMessage> messages, OutgoingPipelineAction next)
        {
            foreach (var header in providers.GetOutgoingHeaders())
            {
                foreach (var message in messages)
                {
                    message.Headers.AddIfMissing(header);
                }
            }

            await next(messages)
                .ConfigureAwait(false);
        }
    }
}
