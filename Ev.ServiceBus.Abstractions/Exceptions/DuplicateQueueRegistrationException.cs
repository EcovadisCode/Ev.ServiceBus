using System;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    [Serializable]
    public class DuplicateQueueRegistrationException : Exception
    {
        public DuplicateQueueRegistrationException(string queueName)
            : base($"The queue '{queueName}' has already been registered. You cannot register the same resource twice.")
        {
            QueueName = queueName;
        }

        public string QueueName { get; }
    }
}
