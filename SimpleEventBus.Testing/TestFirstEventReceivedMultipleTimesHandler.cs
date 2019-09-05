using SimpleEventBus.Abstractions.Outgoing;

namespace SimpleEventBus.Testing
{
    public class TestFirstEventReceivedMultipleTimesHandler : CapturingHandlerBase<TestEventReceivedMultipleTimes>
    {
        public TestFirstEventReceivedMultipleTimesHandler(OutgoingHeaderProviders outgoingHeaderProviders)
            : base(outgoingHeaderProviders)
        {
        }
    }
}
