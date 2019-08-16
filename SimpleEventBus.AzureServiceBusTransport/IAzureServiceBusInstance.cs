using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport
{
    public interface IAzureServiceBusInstance
    {
        bool IsCircuitBreakerTripped { get; }

        Task<IList<Message>> TryReceiveMessages(int maximumMessagesToReturn, bool waitIfQueueEmpty);

        Task<IList<Message>> ReceiveDeferredMessages(IEnumerable<long> sequenceNumbers);

        Task<Exception> TrySend(IList<Message> messages);

        Task Abandon(string lockToken);

        Task Complete(string lockToken);

        Task DeadLetter(string lockToken, string deadLetterReason, string deadLetterReasonDetail);

        Task Defer(string lockToken, string deferralReason, string deferralReasonDetail);

        Task<Exception> TryScheduleMessage(Message message, DateTime scheduledTimeUtc);

        Task TryShutDown();
    }
}
