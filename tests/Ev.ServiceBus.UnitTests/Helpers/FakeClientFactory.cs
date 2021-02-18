using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class FakeClientFactory : IClientFactory<QueueOptions, IQueueClient>
    {
        private readonly List<QueueClientMock> _registeredClients;

        public FakeClientFactory()
        {
            _registeredClients = new List<QueueClientMock>();
        }

        public QueueClientMock GetAssociatedMock(string name, bool isReceiver = false)
        {
            return _registeredClients.FirstOrDefault(o => o.QueueName == name && o.IsReceiver == isReceiver);
        }

        public QueueClientMock[] GetAllRegisteredQueueClients()
        {
            return _registeredClients.ToArray();
        }

        public IQueueClient Create(QueueOptions options, ConnectionSettings connectionSettings)
        {
            var clientMock = new QueueClientMock(options.ResourceId);

            _registeredClients.Add(clientMock);
            return clientMock.QueueClient;
        }
    }

    public class FakeTopicClientFactory : IClientFactory<TopicOptions, ITopicClient>
    {
        private readonly List<TopicClientMock> _registeredClients;

        public FakeTopicClientFactory()
        {
            _registeredClients = new List<TopicClientMock>();
        }

        public TopicClientMock[] GetAllRegisteredTopicClients()
        {
            return _registeredClients.ToArray();
        }

        public ITopicClient Create(TopicOptions options, ConnectionSettings connectionSettings)
        {
            var clientMock = new TopicClientMock(options.ResourceId);

            _registeredClients.Add(clientMock);
            return clientMock.Client;
        }
    }

    public class FakeSubscriptionClientFactory : IClientFactory<SubscriptionOptions, ISubscriptionClient>
    {
        private readonly List<SubscriptionClientMock> _registeredClients;

        public FakeSubscriptionClientFactory()
        {
            _registeredClients = new List<SubscriptionClientMock>();
        }

        public SubscriptionClientMock[] GetAllRegisteredSubscriptionClients()
        {
            return _registeredClients.ToArray();
        }

        public ISubscriptionClient Create(SubscriptionOptions options, ConnectionSettings connectionSettings)
        {
            var clientMock = new SubscriptionClientMock(((SubscriptionOptions)options).SubscriptionName);

            _registeredClients.Add(clientMock);
            return clientMock.Client;
        }
    }
}
