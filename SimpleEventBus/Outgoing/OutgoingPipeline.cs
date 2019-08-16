using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleEventBus.Abstractions.Outgoing;

namespace SimpleEventBus.Outgoing
{
    class OutgoingPipeline : IOutgoingPipeline
    {
        private readonly IEnumerable<IOutgoingBehaviour> behaviours;
        private readonly IMessageSink messageSink;
        private readonly ILogger<OutgoingPipeline> logger;
        private OutgoingPipelineAction pipelineStartingAction;
        private readonly object initialisationLock = new object();

        public OutgoingPipeline(IEnumerable<IOutgoingBehaviour> behaviours, IMessageSink messageSink, ILogger<OutgoingPipeline> logger)
        {
            this.behaviours = behaviours;
            this.messageSink = messageSink;
            this.logger = logger;
        }

        private OutgoingPipelineAction BuildPipeline()
        {
            OutgoingPipelineAction lastAction = messageSink.Sink;
            var nextAction = lastAction;

            foreach (var behaviour in behaviours.Reverse())
            {
                var capturedNextAction = nextAction;

                nextAction = (messages)
                    => behaviour.Process(messages, capturedNextAction);
            }

            foreach (var behaviour in behaviours)
            {
                logger.LogInformation($"Adding {behaviour.GetType().Name} to outgoing pipeline.");
            }

            return nextAction;
        }

        public Task Process(IEnumerable<OutgoingMessage> messages)
        {
            if (pipelineStartingAction == null)
            {
                lock (initialisationLock)
                {
                    if (pipelineStartingAction == null)
                    {
                        pipelineStartingAction = BuildPipeline();
                    }
                }
            }

            return pipelineStartingAction.Invoke(messages);
        }
    }
}
