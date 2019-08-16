using System;
using System.Threading.Tasks;
using SimpleEventBus.Abstractions.Outgoing;

namespace SimpleEventBus.Testing
{
    public class FailingTestEventHandler : CapturingHandlerBase<FailingTestEvent>
    {
        public FailingTestEventHandler(OutgoingHeaderProviders outgoingHeaderProviders)
            : base(outgoingHeaderProviders)
        {
        }

        public override async Task HandleMessage(FailingTestEvent message)
        {
            await base
                .HandleMessage(message)
                .ConfigureAwait(false);

            throw new InvalidOperationException("Try again.");
        }
    }
}
