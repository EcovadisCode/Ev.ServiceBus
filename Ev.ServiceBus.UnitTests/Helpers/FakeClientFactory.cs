using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class FakeQueueClientFactory : IQueueClientFactory
    {
        private readonly List<QueueClientMock> _registeredClients;

        public FakeQueueClientFactory()
        {
            _registeredClients = new List<QueueClientMock>();
        }

        public IQueueClient Create(QueueOptions options)
        {
            var clientMock = new QueueClientMock(options.QueueName);

            _registeredClients.Add(clientMock);
            return clientMock.QueueClient;
        }

        public QueueClientMock GetAssociatedMock(string name)
        {
            return _registeredClients.FirstOrDefault(o => o.QueueName == name);
        }

        public QueueClientMock[] GetAllRegisteredQueueClients()
        {
            return _registeredClients.ToArray();
        }
    }

    public class FakeTopicClientFactory : ITopicClientFactory
    {
        private readonly List<TopicClientMock> _registeredClients;

        public FakeTopicClientFactory()
        {
            _registeredClients = new List<TopicClientMock>();
        }

        public ITopicClient Create(TopicOptions options)
        {
            var clientMock = new TopicClientMock(options.TopicName);

            _registeredClients.Add(clientMock);
            return clientMock.Client;
        }

        public TopicClientMock[] GetAllRegisteredTopicClients()
        {
            return _registeredClients.ToArray();
        }
    }

    public class FakeSubscriptionClientFactory : ISubscriptionClientFactory
    {
        private readonly List<SubscriptionClientMock> _registeredClients;

        public FakeSubscriptionClientFactory()
        {
            _registeredClients = new List<SubscriptionClientMock>();
        }

        public ISubscriptionClient Create(SubscriptionOptions options)
        {
            var clientMock = new SubscriptionClientMock(options.SubscriptionName);

            _registeredClients.Add(clientMock);
            return clientMock.Client;
        }

        public SubscriptionClientMock[] GetAllRegisteredSubscriptionClients()
        {
            return _registeredClients.ToArray();
        }
    }
}
