using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Incoming
{
    public class MockHandler : IHandles<ExampleEvent>
    {
        public ExampleEvent CalledWith { get; private set; }

        public bool ThrowException { get; set; }

        public Task HandleMessage(ExampleEvent message)
        {
            CalledWith = message;

            if (ThrowException)
            {
                throw new InvalidOperationException("Was told to throw.");
            }

            return Task.CompletedTask;
        }
    }
}
