using System.Collections.Generic;

namespace SimpleEventBus.Abstractions.Outgoing
{
    public interface IOutgoingHeaderProvider
    {
        IEnumerable<Header> GetOutgoingHeaders();
    }
}
