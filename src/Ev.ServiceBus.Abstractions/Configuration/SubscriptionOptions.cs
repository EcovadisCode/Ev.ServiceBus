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
                $"{topicName}/Subscriptions/{subscriptionName}",
                ClientType.Subscription,
                strictMode)
        {
            _serviceCollection = serviceCollection;
            SubscriptionName = subscriptionName;
            TopicName = topicName;
        }

        /// <summary>
        /// The name of the subscription.
        /// </summary>
        public string SubscriptionName { get; }

        /// <summary>
        /// The name of the topic.
        /// </summary>
        public string TopicName { get; }
    }
}
