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

        // /// <summary>
        // ///     Registers a queue that can be used to send or receive messages.
        // /// </summary>
        // /// <param name="name">The name of the queue. It must be unique.</param>
        // /// <exception cref="DuplicateQueueRegistrationException"></exception>
        // /// <returns>The options object</returns>
        // public QueueOptions RegisterQueue(string name)
        // {
        //     if (_queues.Any(o => o.QueueName == name))
        //     {
        //         throw new DuplicateQueueRegistrationException(name);
        //     }
        //
        //     var queue = new QueueOptions(name);
        //     _queues.Add(queue);
        //     return queue;
        // }

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

        // /// <summary>
        // ///     Registers a topic that can be used to send messages.
        // /// </summary>
        // /// <param name="name">The name of the topic. It must be unique.</param>
        // /// <exception cref="DuplicateTopicRegistrationException"></exception>
        // /// <returns>The options object</returns>
        // public TopicOptions RegisterTopic(string name)
        // {
        //     if (_topics.Any(o => o.TopicName == name))
        //     {
        //         throw new DuplicateTopicRegistrationException(name);
        //     }
        //
        //     var topic = new TopicOptions(name);
        //     _topics.Add(topic);
        //     return topic;
        // }
        //
        // /// <summary>
        // ///     Registers a subscription that can be used to receive messages.
        // /// </summary>
        // /// <param name="topicName">The name of the topic.</param>
        // /// <param name="subscriptionName">The name of the subscription.</param>
        // /// <exception cref="DuplicateTopicRegistrationException"></exception>
        // /// <returns>The options object</returns>
        // public SubscriptionOptions RegisterSubscription(string topicName, string subscriptionName)
        // {
        //     if (_subscriptions.Any(o => o.TopicName == topicName && o.SubscriptionName == subscriptionName))
        //     {
        //         throw new DuplicateSubscriptionRegistrationException(topicName, subscriptionName);
        //     }
        //
        //     var subscription = new SubscriptionOptions(topicName, subscriptionName);
        //     _subscriptions.Add(subscription);
        //     return subscription;
        // }

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
