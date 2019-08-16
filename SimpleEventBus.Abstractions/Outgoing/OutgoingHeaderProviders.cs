using System.Collections.Generic;
using System.Linq;

namespace SimpleEventBus.Abstractions.Outgoing
{
    public class OutgoingHeaderProviders
    {
        private readonly IOutgoingHeaderProvider[] providers;

        public OutgoingHeaderProviders(IEnumerable<IOutgoingHeaderProvider> providers)
        {
            this.providers = providers.ToArray();
        }

        public IEnumerable<Header> GetOutgoingHeaders()
        {
            foreach (var provider in providers)
            {
                foreach (var header in provider.GetOutgoingHeaders())
                {
                    if (header.Value != null)
                    {
                        yield return header;
                    }
                }
            }
        }

        public string GetValueOrDefault(string headerName)
        {
            string lastValue = null;

            foreach (var header in GetOutgoingHeaders())
            {
                if (header.HeaderName == headerName)
                {
                    lastValue = header.Value;
                }
            }

            return lastValue;
        }
    }
}
