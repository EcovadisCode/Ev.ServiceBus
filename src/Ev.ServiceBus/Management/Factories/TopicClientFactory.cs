using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class TopicClientFactory : IClientFactory<TopicOptions, TopicClient>
    {
        public TopicClient Create(TopicOptions options, ConnectionSettings connectionSettings)
        {
            if (connectionSettings.Connection != null)
            {
                return new TopicClient(
                    connectionSettings.Connection,
                    options.TopicName,
                    connectionSettings.RetryPolicy);
            }

            if (connectionSettings.ConnectionStringBuilder != null)
            {
                return new TopicClient(
                    connectionSettings.ConnectionStringBuilder,
                    connectionSettings.RetryPolicy);
            }

            return new TopicClient(
                connectionSettings.ConnectionString,
                options.TopicName,
                connectionSettings.RetryPolicy);
        }
    }
}
