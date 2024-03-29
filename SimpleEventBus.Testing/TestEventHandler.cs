using SimpleEventBus.Abstractions.Outgoing;

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
