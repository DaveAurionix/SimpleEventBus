using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.Incoming
{
    sealed class ConcurrentMessageLimitingBehaviour : IIncomingBehaviour, IDisposable
    {
        private readonly SemaphoreSlim semaphore;
        private readonly int maximumMessagesProcessedInParallel;

        public ConcurrentMessageLimitingBehaviour(int maximumMessagesProcessedInParallel)
        {
            semaphore = new SemaphoreSlim(maximumMessagesProcessedInParallel);
            this.maximumMessagesProcessedInParallel = maximumMessagesProcessedInParallel;
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }

        public async Task Process(IncomingMessage message, Context context, IncomingPipelineAction nextAction)
        {
            var remainingLockTime = message.RemainingLockTime;
            if (remainingLockTime <= TimeSpan.Zero
                || !await semaphore
                    .WaitAsync(remainingLockTime, context.CancellationToken)
                    .ConfigureAwait(false))
            {
                throw new MessageConcurrencyException(
                    $"The concurrent-processing limit of {maximumMessagesProcessedInParallel} messages was reached and a slot did not become available before the remaining lock time for this message was exceeded.");
            }

            try
            {
                await nextAction(message, context)
                    .ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
