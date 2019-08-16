using System;

namespace SimpleEventBus.Incoming
{
    public class MessageConcurrencyException : Exception
    {
        public MessageConcurrencyException()
        {
        }

        public MessageConcurrencyException(string message) : base(message)
        {
        }

        public MessageConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
