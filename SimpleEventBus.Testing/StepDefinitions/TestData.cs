using System;

namespace SimpleEventBus.Testing.StepDefinitions
{
    public class TestData
    {
        public string TestEventContent { get; } = Guid.NewGuid().ToString();

        public string TestCommandContent { get; } = Guid.NewGuid().ToString();

        public string CorrelationId { get; } = Guid.NewGuid().ToString();
    }
}
