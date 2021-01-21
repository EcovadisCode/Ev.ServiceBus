using System;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    [Serializable]
    public class DuplicateSubscriptionRegistrationException : Exception
    {
        public DuplicateSubscriptionRegistrationException(string topicName, string subscriptionName)
            : base(
                $"The subscription '{topicName}/{subscriptionName}' has already been registered."
                + " You cannot register the same resource twice.")
        {
            TopicName = topicName;
            SubscriptionName = subscriptionName;
        }

        public string TopicName { get; }
        public string SubscriptionName { get; }
    }
}