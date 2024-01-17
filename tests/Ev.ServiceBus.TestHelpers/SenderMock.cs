using System.Collections.Generic;
using System.Threading;
using Azure.Messaging.ServiceBus;
using Moq;

namespace Ev.ServiceBus.TestHelpers;

public class SenderMock
{
    public SenderMock(string queueOrTopicName)
    {
        QueueOrTopicName = queueOrTopicName;
        Mock = new Mock<ServiceBusSender>();
        Mock.SetupGet(o => o.EntityPath).Returns(queueOrTopicName);
        Mock.Setup(o => o.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceBusModelFactory.ServiceBusMessageBatch(0, new List<ServiceBusMessage>()));
    }

    public string QueueOrTopicName { get; }
    public Mock<ServiceBusSender> Mock { get; }
}
