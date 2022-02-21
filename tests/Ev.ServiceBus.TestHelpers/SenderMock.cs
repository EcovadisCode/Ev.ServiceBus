using Azure.Messaging.ServiceBus;
using Moq;

namespace Ev.ServiceBus.TestHelpers;

public class SenderMock
{
    public SenderMock(string queueOrTopicName)
    {
        QueueOrTopicName = queueOrTopicName;
        Mock = new Mock<ServiceBusSender>();
    }

    public string QueueOrTopicName { get; }
    public Mock<ServiceBusSender> Mock { get; }
}
