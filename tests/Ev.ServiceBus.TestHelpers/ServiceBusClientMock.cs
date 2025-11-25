using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions.Configuration;
using Moq;

namespace Ev.ServiceBus.TestHelpers;

public class ServiceBusClientMock
{
    private readonly List<SenderMock> _registeredSenders;
    private readonly List<ProcessorMock> _registeredReceivers;
    private readonly List<SessionProcessorMock> _registeredSessionReceivers;

    public ServiceBusClientMock(ConnectionSettings connectionSettings)
    {
        ConnectionSettings = connectionSettings;
        _registeredSenders = new List<SenderMock>();
        _registeredReceivers = new List<ProcessorMock>();
        _registeredSessionReceivers = new List<SessionProcessorMock>();
        Mock = new Mock<ServiceBusClient>();
        EndPoint = connectionSettings.Endpoint;
        Mock.Setup(o => o.CreateSender(It.IsAny<string>())).Returns<string>((queueOrTopicName) =>
        {
            var senderMock = new SenderMock(queueOrTopicName);
            _registeredSenders.Add(senderMock);
            return senderMock.Mock.Object;
        });
        Mock.Setup(o => o.CreateProcessor(It.IsAny<string>(), It.IsAny<ServiceBusProcessorOptions>()))
            .Returns<string, ServiceBusProcessorOptions>((queueName, options) =>
            {
                var mock = new ProcessorMock(queueName, options);
                _registeredReceivers.Add(mock);
                return mock;
            });
        Mock.Setup(o => o.CreateProcessor(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ServiceBusProcessorOptions>()))
            .Returns<string, string, ServiceBusProcessorOptions>((topicName, subscriptionName, options) =>
            {
                var mock = new ProcessorMock(topicName, subscriptionName, options);
                _registeredReceivers.Add(mock);
                return mock;
            });
        Mock.Setup(o => o.CreateSessionProcessor(It.IsAny<string>(), It.IsAny<ServiceBusSessionProcessorOptions>()))
            .Returns<string, ServiceBusSessionProcessorOptions>((queueName, options) =>
            {
                var mock = new SessionProcessorMock(queueName, options);
                _registeredSessionReceivers.Add(mock);
                return mock;
            });
        Mock.Setup(o => o.CreateSessionProcessor(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ServiceBusSessionProcessorOptions>()))
            .Returns<string, string, ServiceBusSessionProcessorOptions>((topicName, subscriptionName, options) =>
            {
                var mock = new SessionProcessorMock(topicName, subscriptionName, options);
                _registeredSessionReceivers.Add(mock);
                return mock;
            });
    }

    public string EndPoint { get; }
    public Mock<ServiceBusClient> Mock { get; }
    public ConnectionSettings ConnectionSettings { get; }

    public IReadOnlyList<SenderMock> Senders => _registeredSenders;
    public IReadOnlyList<ProcessorMock> Processors => _registeredReceivers;
    public IReadOnlyList<SessionProcessorMock> SessionProcessors => _registeredSessionReceivers;
    public SenderMock GetSenderMock(string resourceId)
    {
        return _registeredSenders.FirstOrDefault(o => o.QueueOrTopicName == resourceId);
    }

    public ProcessorMock GetProcessorMock(string queueName)
    {
        return _registeredReceivers.FirstOrDefault(o => o.ResourceId == queueName);
    }

    public ProcessorMock GetProcessorMock(string topicName, string subscriptionName)
    {
        return _registeredReceivers.FirstOrDefault(o => o.ResourceId == $"{topicName}/Subscriptions/{subscriptionName}");
    }

    public SessionProcessorMock GetSessionProcessorMock(string queueName)
    {
        return _registeredSessionReceivers.FirstOrDefault(o => o.ResourceId == queueName);
    }

    public SessionProcessorMock GetSessionProcessorMock(string topicName, string subscriptionName)
    {
        return _registeredSessionReceivers.FirstOrDefault(o => o.ResourceId == $"{topicName}/Subscriptions/{subscriptionName}");
    }
}
