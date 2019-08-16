namespace SimpleEventBus.Abstractions.Incoming
{
    public interface IBatchSizeProvider
    {
        int CalculateNewRecommendedBatchSize(int numberActuallyRetrievedInLastBatch);
    }
}
