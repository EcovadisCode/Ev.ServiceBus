using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class TopicClientFactory : ITopicClientFactory
    {
        public ITopicClient Create(TopicOptions options)
        {
            if (options.Connection != null)
            {
                return new TopicClient(options.Connection, options.TopicName, options.RetryPolicy);
            }

            if (options.ConnectionStringBuilder != null)
            {
                return new TopicClient(options.ConnectionStringBuilder, options.RetryPolicy);
            }

            return new TopicClient(options.ConnectionString, options.TopicName, options.RetryPolicy);
        }
    }
}
