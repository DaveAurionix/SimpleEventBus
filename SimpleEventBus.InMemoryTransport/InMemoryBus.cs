using SimpleEventBus.Abstractions.Outgoing;
using System;
using System.Collections.Generic;

namespace SimpleEventBus.InMemoryTransport
{
    class InMemoryBus
    {
        readonly List<Action<IEnumerable<OutgoingMessage>>> connectedActions = new List<Action<IEnumerable<OutgoingMessage>>>();

        public void Connect(Action<IEnumerable<OutgoingMessage>> onMessageReceive)
        {
            lock (connectedActions)
            {
                connectedActions.Add(onMessageReceive);
            }
        }

        public void Disconnect(Action<IEnumerable<OutgoingMessage>> onMessageReceive)
        {
            lock (connectedActions)
            {
                connectedActions.Remove(onMessageReceive);
            }
        }

        public void Publish(IEnumerable<OutgoingMessage> messages)
        {
            lock (connectedActions)
            {
                foreach (var action in connectedActions)
                {
                    action(messages);
                }
            }
        }
    }
}
