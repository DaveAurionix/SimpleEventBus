using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Abstractions.Outgoing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.Testing
{
    public abstract class CapturingHandlerBase<TMessageType> : IHandles<TMessageType>
    {
        private readonly List<CapturedMessage<TMessageType>> capturedMessages = new List<CapturedMessage<TMessageType>>();
        private readonly OutgoingHeaderProviders outgoingHeaderProviders;

        protected CapturingHandlerBase(OutgoingHeaderProviders outgoingHeaderProviders)
        {
            this.outgoingHeaderProviders = outgoingHeaderProviders;
        }

        public virtual Task HandleMessage(TMessageType message)
        {
            lock (capturedMessages)
            {
                // TODO Wrong to read incoming headers by checking the outgoing values
                // TODO Constants for header names?
                capturedMessages.Add(
                    new CapturedMessage<TMessageType>(
                        outgoingHeaderProviders.GetValueOrDefault("Correlation-ID"),
                        outgoingHeaderProviders.GetValueOrDefault("DomainUnderTest"),
                        message));
            }

            return Task.CompletedTask;
        }

        public IReadOnlyCollection<CapturedMessage<TMessageType>> ReceivedMessages
        {
            get
            {
                lock (capturedMessages)
                {
                    return new List<CapturedMessage<TMessageType>>(capturedMessages)
                        .AsReadOnly();
                }
            }
        }
    }
}
