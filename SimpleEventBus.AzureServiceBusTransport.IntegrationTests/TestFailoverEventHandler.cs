using SimpleEventBus.Abstractions.Outgoing;
using SimpleEventBus.Testing;

namespace SimpleEventBus.AzureServiceBusTransport.IntegrationTests
{
    public class TestFailoverEventHandler : CapturingHandlerBase<TestFailoverEvent>
    {
        public TestFailoverEventHandler(OutgoingHeaderProviders outgoingHeaderProviders)
            : base(outgoingHeaderProviders)
        {
        }
    }
}
