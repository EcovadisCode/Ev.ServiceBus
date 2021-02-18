using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class SubscriptionOptions : ReceiverOptions
    {
        private readonly IServiceCollection _serviceCollection;

        public SubscriptionOptions(
            IServiceCollection serviceCollection,
            string topicName,
            string subscriptionName,
            bool strictMode)
            : base(
                serviceCollection,
                EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName),
                ClientType.Subscription,
                strictMode)
        {
            _serviceCollection = serviceCollection;
            SubscriptionName = subscriptionName;
            TopicName = topicName;
        }

        public string SubscriptionName { get; }
        public string TopicName { get; }
    }
}
