using Microsoft.Extensions.Logging;
using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventBus.Incoming
{
    internal class BatchSizeGoverningBehaviour : IIncomingBehaviour, IBatchSizeProvider
    {
        public const int MinimumMessageHistoryKept = 20;

        private readonly ConcurrentQueue<bool> finishedWithinSafeLockTime = new ConcurrentQueue<bool>();
        private readonly double batchLockDurationRiskAttitude;
        private readonly ILogger<BatchSizeGoverningBehaviour> logger;
        private int currentRecommendedBatchSize;

        public BatchSizeGoverningBehaviour(double batchLockDurationRiskAttitude, ILogger<BatchSizeGoverningBehaviour> logger, int initialBatchSize = 1)
        {
            this.batchLockDurationRiskAttitude = batchLockDurationRiskAttitude;
            this.logger = logger;
            currentRecommendedBatchSize = initialBatchSize;
        }

        public async Task Process(IncomingMessage message, Context context, IncomingPipelineAction next)
        {
            try
            {
                await next(message, context)
                    .ConfigureAwait(false);
            }
            finally
            {
                finishedWithinSafeLockTime.Enqueue(
                    FinishedWithinSafeTime(message));

                TrimMetrics();
            }
        }

        public bool FinishedWithinSafeTime(IncomingMessage message)
            => message.LockTimeFractionRemaining > batchLockDurationRiskAttitude;

        private void TrimMetrics()
        {
            var targetWindowSize = Math.Max(
                MinimumMessageHistoryKept,
                currentRecommendedBatchSize * 2);
            while (finishedWithinSafeLockTime.Count > targetWindowSize
                && finishedWithinSafeLockTime.TryDequeue(out _))
            {
            }
        }

        public int CalculateNewRecommendedBatchSize(int numberActuallyRetrievedInLastBatch)
        {
            // This may race with the trimmer but the batch size is only adjusted by one so a collision has limited impact and it will self-correct in the next run anyway.
            var count = finishedWithinSafeLockTime.Count;

            if (count < 2)
            {
                return currentRecommendedBatchSize;
            }

            var countFinishedWithinTime = finishedWithinSafeLockTime.Count(finishedOnTime => finishedOnTime);
            var percentFinishedWithinTime = countFinishedWithinTime * 100 / count;

            if (percentFinishedWithinTime >= 95)
            {
                if (numberActuallyRetrievedInLastBatch >= currentRecommendedBatchSize)
                {
                    currentRecommendedBatchSize++;
                    logger.LogDebug($"Increasing batch size to {currentRecommendedBatchSize} as {percentFinishedWithinTime}% of the last {count} messages finished within the safe period and the queue appears to be backlogged.");
                }

                return currentRecommendedBatchSize;
            }

            if (currentRecommendedBatchSize > 1)
            {
                currentRecommendedBatchSize--;
                logger.LogDebug($"Decreasing batch size to {currentRecommendedBatchSize} as only {percentFinishedWithinTime}% of the last {count} messages finished within the safe period.");
            }

            return currentRecommendedBatchSize;
        }
    }
}
