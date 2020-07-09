using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class SubscriptionClientFactory : ISubscriptionClientFactory
    {
        public ISubscriptionClient Create(SubscriptionOptions options)
        {
            if (options.Connection != null)
            {
                return new SubscriptionClient(options.Connection, options.TopicName, options.SubscriptionName, options.ReceiveMode, options.RetryPolicy);
            }

            if (options.ConnectionStringBuilder != null)
            {
                return new SubscriptionClient(options.ConnectionStringBuilder, options.SubscriptionName, options.ReceiveMode, options.RetryPolicy);
            }

            return new SubscriptionClient(options.ConnectionString, options.TopicName, options.SubscriptionName, options.ReceiveMode, options.RetryPolicy);
        }
    }
}
