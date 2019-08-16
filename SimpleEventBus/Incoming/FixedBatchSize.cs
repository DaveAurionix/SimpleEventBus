using SimpleEventBus.Abstractions.Incoming;

namespace SimpleEventBus.Incoming
{
    class FixedBatchSize : IBatchSizeProvider
    {
        private readonly int batchSize;

        public FixedBatchSize(int batchSize)
        {
            this.batchSize = batchSize;
        }

        public int CalculateNewRecommendedBatchSize(int numberActuallyRetrievedInLastBatch)
            => batchSize;
    }
}
