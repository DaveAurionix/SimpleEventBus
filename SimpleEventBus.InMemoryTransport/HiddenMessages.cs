using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SimpleEventBus.InMemoryTransport
{
    class HiddenMessages
    {
        private readonly List<Tuple<DateTime, QueuedMessage>> messages = new List<Tuple<DateTime, QueuedMessage>>();

        public void Add(DateTime becomesVisibleAtUtc, QueuedMessage message)
        {
            messages.Add(Tuple.Create(becomesVisibleAtUtc, message));
        }

        public void MoveAnyDueHiddenMessagesTo(ConcurrentQueue<QueuedMessage> targetQueue)
        {
            if (messages.Count < 1)
            {
                return;
            }

            lock (messages)
            {
                var messagesToRestore = messages
                    .Where(item => item.Item1 <= DateTime.UtcNow)
                    .ToArray();

                foreach (var message in messagesToRestore)
                {
                    messages.Remove(message);
                    targetQueue.Enqueue(message.Item2);
                }
            }
        }

        public QueuedMessage Remove(string messageId)
        {
            lock (messages)
            {
                var originalMessage = messages.Single(item => item.Item2.Id == messageId);
                messages.Remove(originalMessage);
                return originalMessage.Item2;
            }
        }

        public void RefreshBecomesVisibleAt(string messageId, DateTime newBecomesVisibleAtUtc)
        {
            lock (messages)
            {
                var itemToRemove = messages.Single(item => item.Item2.Id == messageId);
                messages.Remove(itemToRemove);
                messages.Add(Tuple.Create(newBecomesVisibleAtUtc, itemToRemove.Item2));
            }
        }
    }
}
