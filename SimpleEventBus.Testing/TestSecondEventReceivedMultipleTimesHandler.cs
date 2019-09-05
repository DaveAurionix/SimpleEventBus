using SimpleEventBus.Abstractions.Outgoing;

namespace SimpleEventBus.Testing
{
    public class TestSecondEventReceivedMultipleTimesHandler : CapturingHandlerBase<TestEventReceivedMultipleTimes>
    {
        public TestSecondEventReceivedMultipleTimesHandler(OutgoingHeaderProviders outgoingHeaderProviders)
            : base(outgoingHeaderProviders)
        {
        }
    }
}
