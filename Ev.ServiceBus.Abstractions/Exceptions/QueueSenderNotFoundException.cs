using System;

namespace Ev.ServiceBus.Abstractions.Exceptions
{
    public class QueueSenderNotFoundException : Exception
    {
        public QueueSenderNotFoundException(string queueName)
            : base(
                $"The queue '{queueName}' you tried to retrieve was not found. "
                + "Verify your configuration to make sure the queue is properly registered.")
        {
            QueueName = queueName;
        }

        public string QueueName { get; }
    }
}
