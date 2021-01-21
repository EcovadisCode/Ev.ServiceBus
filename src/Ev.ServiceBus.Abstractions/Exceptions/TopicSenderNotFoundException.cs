using System;

namespace Ev.ServiceBus.Abstractions
{
    [Serializable]
    public class TopicSenderNotFoundException : Exception
    {
        public TopicSenderNotFoundException(string topicName)
            : base(
                $"The topic '{topicName}' you tried to retrieve was not found. "
                + "Verify your configuration to make sure the topic is properly registered.")
        {
            TopicName = topicName;
        }

        public string TopicName { get; }
    }
}
