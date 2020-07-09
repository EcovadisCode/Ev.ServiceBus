using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class QueueClientFactory : IQueueClientFactory
    {
        public IQueueClient Create(QueueOptions options)
        {
            if (options.Connection != null)
            {
                return new QueueClient(options.Connection, options.QueueName, options.ReceiveMode, options.RetryPolicy);
            }

            if (options.ConnectionStringBuilder != null)
            {
                return new QueueClient(options.ConnectionStringBuilder, options.ReceiveMode, options.RetryPolicy);
            }

            return new QueueClient(options.ConnectionString, options.QueueName, options.ReceiveMode, options.RetryPolicy);
        }
    }
}
