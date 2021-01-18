using System.Collections.Generic;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus
{
    public class ServiceBusRegistry : IServiceBusRegistry
    {
        private readonly SortedList<string, QueueWrapper> _queues;
        private readonly SortedList<string, SubscriptionWrapper> _subscriptions;
        private readonly SortedList<string, TopicWrapper> _topics;

        public ServiceBusRegistry()
        {
            _queues = new SortedList<string, QueueWrapper>();
            _topics = new SortedList<string, TopicWrapper>();
            _subscriptions = new SortedList<string, SubscriptionWrapper>();
        }

        public IMessageSender GetQueueSender(string name)
        {
            if (_queues.TryGetValue(name, out var queue))
            {
                return queue.Sender;
            }

            throw new QueueSenderNotFoundException(name);
        }

        public IMessageSender GetTopicSender(string name)
        {
            if (_topics.TryGetValue(name, out var topic))
            {
                return topic.Sender;
            }

            throw new TopicSenderNotFoundException(name);
        }

        internal void Register(QueueWrapper queue)
        {
            _queues.Add(queue.Options.EntityPath, queue);
        }

        internal void Register(SubscriptionWrapper subscription)
        {
            _subscriptions.Add(subscription.Options.EntityPath, subscription);
        }

        internal void Register(TopicWrapper topic)
        {
            _topics.Add(topic.Options.EntityPath, topic);
        }

        internal IList<QueueWrapper> GetAllQueues()
        {
            return _queues.Values;
        }

        internal IList<TopicWrapper> GetAllTopics()
        {
            return _topics.Values;
        }

        internal IList<SubscriptionWrapper> GetAllSubscriptions()
        {
            return _subscriptions.Values;
        }
    }
}
