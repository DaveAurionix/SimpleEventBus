using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleEventBus.Abstractions;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Incoming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Incoming
{
    [TestClass]
    public sealed class ConcurrentMessageLimitingBehaviourShould : IDisposable
    {
        const int MaximumConcurrency = 2;
        private ConcurrentMessageLimitingBehaviour behaviour;
        int actionStartedCount;
        int actionCompletedCount;

        [TestInitialize]
        public void Setup()
        {
            behaviour = new ConcurrentMessageLimitingBehaviour(MaximumConcurrency);
        }

        [TestCleanup]
        public void Dispose()
        {
            behaviour.Dispose();
        }

        [TestMethod]
        public async Task InvokeTheNextBehaviourInTheChain()
        {
            await Process(1).ConfigureAwait(false);

            Assert.AreEqual(1, actionCompletedCount);
        }

        [TestMethod]
        public async Task LimitConcurrentlyExecutingHandlers()
        {
            var allowedTasks = StartLongRunning(MaximumConcurrency);
            Assert.AreEqual(MaximumConcurrency, actionStartedCount);
            Assert.AreEqual(0, actionCompletedCount);

            var blockedTask = StartLongRunning(1);
            Assert.AreEqual(MaximumConcurrency, actionStartedCount);
            Assert.AreEqual(0, actionCompletedCount);

            await Task.Delay(TimeSpan.FromSeconds(2.5)).ConfigureAwait(false);
            Assert.AreEqual(MaximumConcurrency + 1, actionStartedCount);
            Assert.AreEqual(MaximumConcurrency, actionCompletedCount);

            await Task.Delay(TimeSpan.FromSeconds(2.5)).ConfigureAwait(false);
            Assert.AreEqual(MaximumConcurrency + 1, actionStartedCount);
            Assert.AreEqual(MaximumConcurrency + 1, actionCompletedCount);

            await Task
                .WhenAll(allowedTasks.Concat(blockedTask))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ThrowExceptionIfMessageExceedsLockTimeDuringWaitPeriod()
        {
            var allowedTasks = StartLongRunning(MaximumConcurrency);
            var blockedTask = behaviour.Process(
                IncomingMessageBuilder
                    .New()
                    .WithLockExpiry(DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromSeconds(0.25))
                    .Build(),
                new Context(null),
                LongRunningNextAction);

            await Task.WhenAll(allowedTasks).ConfigureAwait(false);

            var exception = await Assert
                .ThrowsExceptionAsync<MessageConcurrencyException>(() => blockedTask)
                .ConfigureAwait(false);

            Assert.AreEqual($"The concurrent-processing limit of {MaximumConcurrency} messages was reached and a slot did not become available before the remaining lock time for this message was exceeded.", exception.Message);
        }

        private Task NextAction(IncomingMessage message, Context context)
        {
            Interlocked.Increment(ref actionStartedCount);
            Interlocked.Increment(ref actionCompletedCount);
            return Task.CompletedTask;
        }

        private async Task LongRunningNextAction(IncomingMessage message, Context context)
        {
            Interlocked.Increment(ref actionStartedCount);

            await Task
                .Delay(TimeSpan.FromSeconds(2))
                .ConfigureAwait(false);

            Interlocked.Increment(ref actionCompletedCount);
        }

        private async Task Process(int count)
        {
            for (var index = 0; index < count; index++)
            {
                await behaviour
                    .Process(
                        IncomingMessageBuilder.BuildDefault(),
                        new Context(null),
                        NextAction)
                    .ConfigureAwait(false);
            }
        }

        private List<Task> StartLongRunning(int count)
        {
            var tasks = new List<Task>();

            for (var index = 0; index < count; index++)
            {
                tasks.Add(
                    behaviour
                        .Process(
                            IncomingMessageBuilder.BuildDefault(),
                            new Context(null),
                            LongRunningNextAction));
            }

            return tasks;
        }
    }
}
