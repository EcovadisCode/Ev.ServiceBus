using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class QueueClientFactory : IQueueClientFactory
    {
        public IClientEntity Create(ClientOptions options, ConnectionSettings connectionSettings)
        {
            if (connectionSettings.Connection != null)
            {
                return new QueueClient(
                    connectionSettings.Connection,
                    options.EntityPath,
                    connectionSettings.ReceiveMode,
                    connectionSettings.RetryPolicy);
            }

            if (connectionSettings.ConnectionStringBuilder != null)
            {
                return new QueueClient(
                    connectionSettings.ConnectionStringBuilder,
                    connectionSettings.ReceiveMode,
                    connectionSettings.RetryPolicy);
            }

            return new QueueClient(
                connectionSettings.ConnectionString,
                options.EntityPath,
                connectionSettings.ReceiveMode,
                connectionSettings.RetryPolicy);
        }
    }
}
