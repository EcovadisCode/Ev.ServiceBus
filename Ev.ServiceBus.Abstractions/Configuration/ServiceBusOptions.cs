using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ev.ServiceBus")]

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class ServiceBusOptions
    {
        private readonly List<QueueOptions> _queues;
        private readonly List<SubscriptionOptions> _subscriptions;
        private readonly List<TopicOptions> _topics;

        public ServiceBusOptions()
        {
            _queues = new List<QueueOptions>();
            Queues = new ReadOnlyCollection<QueueOptions>(_queues);
            _topics = new List<TopicOptions>();
            Topics = new ReadOnlyCollection<TopicOptions>(_topics);
            _subscriptions = new List<SubscriptionOptions>();
            Subscriptions = new ReadOnlyCollection<SubscriptionOptions>(_subscriptions);
        }

        internal bool Enabled { get; set; }
        internal bool ReceiveMessages { get; set; }

        public ReadOnlyCollection<QueueOptions> Queues { get; }
        public ReadOnlyCollection<TopicOptions> Topics { get; }
        public ReadOnlyCollection<SubscriptionOptions> Subscriptions { get; }

        /// <summary>
        ///     Registers a queue that can be used to send or receive messages.
        /// </summary>
        /// <param name="name">The name of the queue. It must be unique.</param>
        /// <exception cref="DuplicateQueueRegistrationException"></exception>
        /// <returns>The options object</returns>
        public QueueOptions RegisterQueue(string name)
        {
            if (_queues.Any(o => o.QueueName == name))
            {
                throw new DuplicateQueueRegistrationException(name);
            }

            var queue = new QueueOptions(name);
            _queues.Add(queue);
            return queue;
        }

        /// <summary>
        ///     Registers a topic that can be used to send messages.
        /// </summary>
        /// <param name="name">The name of the topic. It must be unique.</param>
        /// <exception cref="DuplicateTopicRegistrationException"></exception>
        /// <returns>The options object</returns>
        public TopicOptions RegisterTopic(string name)
        {
            if (_topics.Any(o => o.TopicName == name))
            {
                throw new DuplicateTopicRegistrationException(name);
            }

            var topic = new TopicOptions(name);
            _topics.Add(topic);
            return topic;
        }

        /// <summary>
        ///     Registers a subscription that can be used to receive messages.
        /// </summary>
        /// <param name="topicName">The name of the topic.</param>
        /// <param name="subscriptionName">The name of the subscription.</param>
        /// <exception cref="DuplicateTopicRegistrationException"></exception>
        /// <returns>The options object</returns>
        public SubscriptionOptions RegisterSubscription(string topicName, string subscriptionName)
        {
            if (_subscriptions.Any(o => o.TopicName == topicName && o.SubscriptionName == subscriptionName))
            {
                throw new DuplicateSubscriptionRegistrationException(topicName, subscriptionName);
            }

            var subscription = new SubscriptionOptions(topicName, subscriptionName);
            _subscriptions.Add(subscription);
            return subscription;
        }
    }
}
