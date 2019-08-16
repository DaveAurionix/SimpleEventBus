using System;
using System.Collections.Generic;

namespace SimpleEventBus.AzureServiceBusTransport
{
    public class AzureServiceBusTransportSettings
    {
        public IEnumerable<string> ConnectionStrings { get; set; }

        public string TopicName { get; set; }

        public bool EnablePartitioning { get; set; }

        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public TimeSpan LongWaitReadTimeout { get; set; } = TimeSpan.FromMinutes(15);

        public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public TimeSpan BackoffDelayIfAllConnectionsFaulty { get; set; } = TimeSpan.FromSeconds(45);

        public TimeSpan BackoffDelayForFaultyConnection { get; set; } = TimeSpan.FromMinutes(2);

        public string EffectiveTopicName
            => TopicName?.Replace("{MachineName}", Environment.MachineName);

        public string SafeEffectiveTopicName
            => EffectiveTopicName.Replace('\'', '-');

        public AzureServiceBusTransportSettings Clone()
            => (AzureServiceBusTransportSettings)MemberwiseClone();
    }
}
