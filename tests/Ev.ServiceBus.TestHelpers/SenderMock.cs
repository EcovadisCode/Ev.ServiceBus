using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Moq;

namespace Ev.ServiceBus.TestHelpers;

public class SenderMock
{
    public List<ServiceBusMessage> MessagesSent { get; private set; } = new();

    public SenderMock(string queueOrTopicName)
    {
        QueueOrTopicName = queueOrTopicName;
        Mock = new Mock<ServiceBusSender>();
        Mock.SetupGet(o => o.EntityPath).Returns(queueOrTopicName);
        Mock.Setup(o => o.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceBusModelFactory.ServiceBusMessageBatch(0, new List<ServiceBusMessage>()));
        Mock
            .Setup(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((ServiceBusMessage message, CancellationToken token) =>
            {
                MessagesSent.Add(message);
            });
    }

    public string QueueOrTopicName { get; }
    public Mock<ServiceBusSender> Mock { get; }
}
