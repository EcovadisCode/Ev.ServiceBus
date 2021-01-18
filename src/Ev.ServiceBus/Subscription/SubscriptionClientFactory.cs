using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class SubscriptionClientFactory : ISubscriptionClientFactory
    {
        public IClientEntity Create(ClientOptions options, ConnectionSettings connectionSettings)
        {
            if (connectionSettings.Connection != null)
            {
                return new SubscriptionClient(
                    connectionSettings.Connection,
                    options.EntityPath,
                    ((SubscriptionOptions) options).SubscriptionName,
                    connectionSettings.ReceiveMode,
                    connectionSettings.RetryPolicy);
            }

            if (connectionSettings.ConnectionStringBuilder != null)
            {
                return new SubscriptionClient(
                    connectionSettings.ConnectionStringBuilder,
                    ((SubscriptionOptions) options).SubscriptionName,
                    connectionSettings.ReceiveMode,
                    connectionSettings.RetryPolicy);
            }

            return new SubscriptionClient(
                connectionSettings.ConnectionString,
                options.EntityPath,
                ((SubscriptionOptions) options).SubscriptionName,
                connectionSettings.ReceiveMode,
                connectionSettings.RetryPolicy);
        }
    }
}
