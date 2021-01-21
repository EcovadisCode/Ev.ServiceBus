using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ev.ServiceBus")]

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
            Settings = new ServiceBusSettings();
        }

        public ServiceBusSettings Settings { get; }

        public ReadOnlyCollection<QueueOptions> Queues { get; }
        public ReadOnlyCollection<TopicOptions> Topics { get; }
        public ReadOnlyCollection<SubscriptionOptions> Subscriptions { get; }

        /// <summary>
        ///     Registers a queue that can be used to send or receive messages.
        /// </summary>
        /// <param name="queue">The name of the queue. It must be unique.</param>
        /// <exception cref="DuplicateQueueRegistrationException"></exception>
        internal void RegisterQueue(QueueOptions queue)
        {
            if (_queues.Any(o => o.QueueName == queue.QueueName))
            {
                throw new DuplicateQueueRegistrationException(queue.QueueName);
            }

            _queues.Add(queue);
        }

        /// <summary>
        ///     Registers a topic that can be used to send messages.
        /// </summary>
        /// <param name="topic">The name of the topic. It must be unique.</param>
        /// <exception cref="DuplicateTopicRegistrationException"></exception>
        internal void RegisterTopic(TopicOptions topic)
        {
            if (_topics.Any(o => o.TopicName == topic.TopicName))
            {
                throw new DuplicateTopicRegistrationException(topic.TopicName);
            }

            _topics.Add(topic);
        }

        /// <summary>
        ///     Registers a subscription that can be used to receive messages.
        /// </summary>
        /// <param name="subscription">The name of the topic.</param>
        /// <exception cref="DuplicateTopicRegistrationException"></exception>
        internal void RegisterSubscription(SubscriptionOptions subscription)
        {
            if (_subscriptions.Any(o => o.TopicName == subscription.TopicName && o.SubscriptionName == subscription.SubscriptionName))
            {
                throw new DuplicateSubscriptionRegistrationException(subscription.TopicName, subscription.SubscriptionName);
            }

            _subscriptions.Add(subscription);
        }
    }
}
