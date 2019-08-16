using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.Abstractions.Incoming
{
    public interface IMessageSource
    {
        Task EnsureSubscribed(SubscriptionDescription subscription, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<IncomingMessage>> WaitForNextMessageBatch(int maximumMessagesToReturn, CancellationToken cancellationToken);

        Task Abandon(IncomingMessage message);

        Task Complete(IncomingMessage message);

        Task DeadLetter(IncomingMessage message, string deadLetterReason, string deadLetterReasonDetail);

        Task DeferUntil(IncomingMessage message, DateTime scheduledTimeUtc, string deferralReason, string deferralReasonDetail);

        Task Close();
    }
}
