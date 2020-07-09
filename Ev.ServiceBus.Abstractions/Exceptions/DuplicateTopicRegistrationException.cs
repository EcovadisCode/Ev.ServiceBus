using System;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class DuplicateTopicRegistrationException : Exception
    {
        public DuplicateTopicRegistrationException(string topicName)
            : base($"The topic '{topicName}' has already been registered. You cannot register the same resource twice.")
        {
            TopicName = topicName;
        }

        public string TopicName { get; }
    }
}