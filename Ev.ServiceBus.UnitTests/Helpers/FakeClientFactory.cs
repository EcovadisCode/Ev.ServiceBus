using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class FakeClientFactory : IClientFactory
    {
        private readonly List<QueueClientMock> _registeredClients;

        public FakeClientFactory()
        {
            _registeredClients = new List<QueueClientMock>();
        }

        public QueueClientMock GetAssociatedMock(string name)
        {
            return _registeredClients.FirstOrDefault(o => o.QueueName == name);
        }

        public QueueClientMock[] GetAllRegisteredQueueClients()
        {
            return _registeredClients.ToArray();
        }

        public IClientEntity Create(ClientOptions options, ConnectionSettings connectionSettings)
        {
            var clientMock = new QueueClientMock(options.EntityPath);

            _registeredClients.Add(clientMock);
            return clientMock.QueueClient;
        }
    }

    public class FakeTopicClientFactory : ITopicClientFactory
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

        public IClientEntity Create(ClientOptions options, ConnectionSettings connectionSettings)
        {
            var clientMock = new TopicClientMock(options.EntityPath);

            _registeredClients.Add(clientMock);
            return clientMock.Client;
        }
    }

    public class FakeSubscriptionClientFactory : ISubscriptionClientFactory
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

        public IClientEntity Create(ClientOptions options, ConnectionSettings connectionSettings)
        {
            var clientMock = new SubscriptionClientMock(((SubscriptionOptions)options).SubscriptionName);

            _registeredClients.Add(clientMock);
            return clientMock.Client;
        }
    }
}
