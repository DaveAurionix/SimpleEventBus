using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Testing;

namespace SimpleEventBus.Testing
{
    public class TestEventHandler : CapturingHandlerBase<TestEvent>
    {
        public TestEventHandler(OutgoingHeaderProviders outgoingHeaderProviders)
            : base(outgoingHeaderProviders)
        {
        }
    }
}
