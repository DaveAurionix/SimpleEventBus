using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace SimpleEventBus.Abstractions.Incoming
{
    public class Context
    {
        public Context(IServiceScope serviceScope, CancellationToken cancellationToken = default)
        {
            ServiceScope = serviceScope;
            CancellationToken = cancellationToken;
        }

        public IServiceScope ServiceScope { get; }
        public CancellationToken CancellationToken { get; }
    }
}
