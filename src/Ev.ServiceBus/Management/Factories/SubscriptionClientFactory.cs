using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class SubscriptionClientFactory : IClientFactory<SubscriptionOptions, SubscriptionClient>
    {
        public SubscriptionClient Create(SubscriptionOptions options, ConnectionSettings connectionSettings)
        {
            if (connectionSettings.Connection != null)
            {
                return new SubscriptionClient(
                    connectionSettings.Connection,
                    options.TopicName,
                    options.SubscriptionName,
                    connectionSettings.ReceiveMode,
                    connectionSettings.RetryPolicy);
            }

            if (connectionSettings.ConnectionStringBuilder != null)
            {
                return new SubscriptionClient(
                    connectionSettings.ConnectionStringBuilder,
                    options.SubscriptionName,
                    connectionSettings.ReceiveMode,
                    connectionSettings.RetryPolicy);
            }

            return new SubscriptionClient(
                connectionSettings.ConnectionString,
                options.TopicName,
                options.SubscriptionName,
                connectionSettings.ReceiveMode,
                connectionSettings.RetryPolicy);
        }
    }
}
