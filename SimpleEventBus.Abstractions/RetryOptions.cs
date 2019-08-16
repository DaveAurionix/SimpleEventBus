using System;

namespace SimpleEventBus
{
    public class RetryOptions
    {
        private static readonly Random randomGenerator = new Random();

        public int MaximumImmediateAttempts { get; set; } = 3;

        public TimeSpan DeferredRetryInterval { get; set; } = TimeSpan.FromMinutes(1);

        public int MaximumDeferredAttempts { get; set; } = 3;

        public double MaximumDeferredRetryJitterFactor { get; set; } = 0.1;

        public TimeSpan EffectiveDeferredRetryInterval
            => TimeSpan.FromSeconds(
                DeferredRetryInterval.TotalSeconds
                * (1.0 - (MaximumDeferredRetryJitterFactor / 2.0) + randomGenerator.NextDouble() * MaximumDeferredRetryJitterFactor));
    }
}
