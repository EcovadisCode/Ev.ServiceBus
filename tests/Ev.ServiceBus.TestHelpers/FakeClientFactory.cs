using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions.Configuration;

namespace Ev.ServiceBus.TestHelpers;

public class FakeClientFactory : IClientFactory
{
    private readonly List<ServiceBusClientMock> _registeredClients;

    public FakeClientFactory()
    {
        _registeredClients = new List<ServiceBusClientMock>();
    }

    public ServiceBusClientMock GetAssociatedMock(string endpoint)
    {
        return _registeredClients.FirstOrDefault(o => o.EndPoint == endpoint);
    }

    public ServiceBusClientMock[] GetAllRegisteredClients()
    {
        return _registeredClients.ToArray();
    }

    public ServiceBusClient Create(ConnectionSettings connectionSettings)
    {
        var client = new ServiceBusClientMock(connectionSettings);
        _registeredClients.Add(client);
        return client.Mock.Object;
    }

    public SenderMock GetSenderMock(string resourceId)
    {
        return _registeredClients.Select(o => o.GetSenderMock(resourceId)).FirstOrDefault(o => o != null);
    }

    public ProcessorMock GetProcessorMock(string queueName)
    {
        return _registeredClients.Select(o => o.GetProcessorMock(queueName)).FirstOrDefault(o => o != null);
    }

    public ProcessorMock GetProcessorMock(string topicName, string subscriptionName)
    {
        return _registeredClients.Select(o => o.GetProcessorMock(topicName, subscriptionName)).FirstOrDefault(o => o != null);
    }

    public SessionProcessorMock GetSessionProcessorMock(string queueName)
    {
        return _registeredClients.Select(o => o.GetSessionProcessorMock(queueName)).FirstOrDefault(o => o != null);
    }

    public SessionProcessorMock GetSessionProcessorMock(string topicName, string subscriptionName)
    {
        return _registeredClients.Select(o => o.GetSessionProcessorMock(topicName, subscriptionName)).FirstOrDefault(o => o != null);
    }

    public SenderMock[] GetAllSenderMocks()
    {
        return _registeredClients.SelectMany(o => o.Senders).ToArray();
    }

    public ProcessorMock[] GetAllProcessorMocks()
    {
        return _registeredClients.SelectMany(o => o.Processors).ToArray();
    }

    public SessionProcessorMock[] GetAllSessionProcessorMocks()
    {
        return _registeredClients.SelectMany(o => o.SessionProcessors).ToArray();
    }
}