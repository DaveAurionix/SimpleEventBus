using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Incoming;
using System;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Incoming
{
    [TestClass]
    public class BatchSizeGoverningBehaviourShould
    {
        enum ErrorDirection
        {
            EnsureAtLeast,
            EnsureAtMost
        }

        const int InitialBatchSize = 2;
        
        BatchSizeGoverningBehaviour behaviour;
        bool nextActionWasCalled = false;

        [TestInitialize]
        public void Setup()
        {
            behaviour = new BatchSizeGoverningBehaviour(0.5, NullLogger<BatchSizeGoverningBehaviour>.Instance, InitialBatchSize);
        }

        [TestMethod]
        public async Task InvokeTheNextBehaviourInTheChain()
        {
            await behaviour
                .Process(
                    IncomingMessageBuilder.BuildDefault(),
                    new Context(null),
                    NextAction)
                .ConfigureAwait(false);

            Assert.IsTrue(nextActionWasCalled);
        }

        [TestMethod]
        public void InitiallyReturnTheInitialBatchSize()
        {
            Assert.AreEqual(InitialBatchSize, behaviour.CalculateNewRecommendedBatchSize(10));
        }

        [TestMethod]
        public async Task ReturnInitialBatchSizeWhenOnlyOneMessageHasBeenProcessed()
        {
            await Process(1, IncomingMessageBuilder.BuildDefault).ConfigureAwait(false);

            Assert.AreEqual(InitialBatchSize, behaviour.CalculateNewRecommendedBatchSize(10));
        }

        [TestMethod]
        public async Task KeepSameBatchSizeIf95PercentFinishedOnTimeAndThereIsNoQueuePressure()
        {
            await ProcessWithPercentExpired(5, ErrorDirection.EnsureAtMost).ConfigureAwait(false);

            Assert.AreEqual(InitialBatchSize, behaviour.CalculateNewRecommendedBatchSize(InitialBatchSize - 1));
        }

        [TestMethod]
        public async Task IncreaseBatchSizeIf95PercentFinishedOnTimeAndThereIsQueuePressure()
        {
            await ProcessWithPercentExpired(5, ErrorDirection.EnsureAtMost).ConfigureAwait(false);

            Assert.AreEqual(InitialBatchSize + 1, behaviour.CalculateNewRecommendedBatchSize(InitialBatchSize));
        }

        [TestMethod]
        public async Task DecreaseBatchSizeIf94PercentFinishedOnTime()
        {
            await ProcessWithPercentExpired(6, ErrorDirection.EnsureAtLeast).ConfigureAwait(false);

            Assert.AreEqual(InitialBatchSize - 1, behaviour.CalculateNewRecommendedBatchSize(InitialBatchSize));
        }

        [TestMethod]
        public async Task KeepSameBatchSizeIf94PercentFinishedOnTimeAndBatchSizeIsOne()
        {
            behaviour = new BatchSizeGoverningBehaviour(0.5, NullLogger<BatchSizeGoverningBehaviour>.Instance, initialBatchSize: 1);
            await ProcessWithPercentExpired(6, ErrorDirection.EnsureAtLeast).ConfigureAwait(false);

            Assert.AreEqual(1, behaviour.CalculateNewRecommendedBatchSize(2));
        }

        [TestMethod]
        public void ConsiderRemainingLockTimeRatioGreaterThanRiskFactorAsASafeFinish()
        {
            behaviour = new BatchSizeGoverningBehaviour(0.5, NullLogger<BatchSizeGoverningBehaviour>.Instance, initialBatchSize: 1);
            Assert.IsTrue(
                behaviour.FinishedWithinSafeTime(
                    IncomingMessageBuilder.BuildDefault()));
        }

        [TestMethod]
        public void ConsiderRemainingLockTimeRatioLessThanRiskFactorAsAnUnsafeFinish()
        {
            behaviour = new BatchSizeGoverningBehaviour(0.5, NullLogger<BatchSizeGoverningBehaviour>.Instance, initialBatchSize: 1);
            Assert.IsFalse(
                behaviour.FinishedWithinSafeTime(
                    IncomingMessageBuilder.BuildExpired()));
        }

        private Task NextAction(IncomingMessage message, Context context)
        {
            nextActionWasCalled = true;
            return Task.CompletedTask;
        }

        private async Task ProcessWithPercentExpired(int percentExpired, ErrorDirection errorDirection)
        {
            var countExpired = BatchSizeGoverningBehaviour.MinimumMessageHistoryKept * percentExpired / 100.0;

            var countExpiredInteger = (int)(errorDirection == ErrorDirection.EnsureAtMost ? Math.Floor(countExpired) : Math.Ceiling(countExpired));
            var countSafe = BatchSizeGoverningBehaviour.MinimumMessageHistoryKept - countExpiredInteger;

            await Process(countSafe, IncomingMessageBuilder.BuildDefault).ConfigureAwait(false);
            await Process(countExpiredInteger, IncomingMessageBuilder.BuildExpired).ConfigureAwait(false);
        }

        private async Task Process(int count, Func<IncomingMessage> messageFactory)
        {
            for (var index = 0; index < count; index++)
            {
                await behaviour
                    .Process(
                        messageFactory(),
                        new Context(null),
                        NextAction)
                    .ConfigureAwait(false);
            }
        }
    }
}
