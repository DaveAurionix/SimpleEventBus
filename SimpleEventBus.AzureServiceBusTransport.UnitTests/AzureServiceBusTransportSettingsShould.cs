using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    [TestClass]
    public class AzureServiceBusTransportSettingsShould
    {
        readonly AzureServiceBusTransportSettings settings = new AzureServiceBusTransportSettings();

        [TestMethod]
        public void ReplaceMachineNameInEffectiveTopicName()
        {
            settings.TopicName = "Hello-{MachineName}";
            Assert.AreEqual("Hello-" + Environment.MachineName, settings.EffectiveTopicName);
        }

        [DataTestMethod]
        [DataRow("'","-")]
        public void ReplaceUnsafeCharactersWithHyphensInSafeEffectiveTopicName(string topicName, string expectedSafeEffectiveTopicName)
        {
            settings.TopicName = topicName;
            Assert.AreEqual(expectedSafeEffectiveTopicName, settings.SafeEffectiveTopicName);
        }
    }
}
