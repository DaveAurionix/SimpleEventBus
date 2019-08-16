using SimpleEventBus.Abstractions.Outgoing;

namespace SimpleEventBus.Testing
{
    public class TestCommandHandler : CapturingHandlerBase<TestCommand>
    {
        public TestCommandHandler(OutgoingHeaderProviders outgoingHeaderProviders)
            : base(outgoingHeaderProviders)
        {
        }
    }
}
