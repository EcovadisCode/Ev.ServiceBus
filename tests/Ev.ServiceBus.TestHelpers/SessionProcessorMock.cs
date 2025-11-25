using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Moq;

namespace Ev.ServiceBus.TestHelpers;

public class SessionProcessorMock : ServiceBusSessionProcessor
{
    private readonly ServiceBusProcessor _innerProcessor;

    public SessionProcessorMock(string queueName, ServiceBusSessionProcessorOptions options)
    {
        ResourceId = queueName;
        Options = options;
        _innerProcessor = new ProcessorMock(queueName, null);
    }

    public SessionProcessorMock(string topicName, string subscriptionName, ServiceBusSessionProcessorOptions options)
    {
        ResourceId = $"{topicName}/Subscriptions/{subscriptionName}";
        Options = options;
        _innerProcessor = new ProcessorMock(topicName, subscriptionName, null);
    }

    public string ResourceId { get; }
    public ServiceBusSessionProcessorOptions Options { get; }

    public Task TriggerMessageReception(ServiceBusMessage message, CancellationToken token)
    {
        return TriggerMessageReception(message, token, new Mock<ServiceBusSessionReceiver>().Object);
    }

    public async Task TriggerMessageReception(ServiceBusMessage message, CancellationToken token, ServiceBusSessionReceiver receiver)
    {
        ServiceBusModelFactory.ServiceBusReceivedMessage();
        var ctor = typeof(ServiceBusReceivedMessage).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
            null, new []{typeof(AmqpAnnotatedMessage)}, null);
        var amqpAnnotatedMessage = message.GetRawAmqpMessage();
        amqpAnnotatedMessage.Header.DeliveryCount = 1;
        var obj = (ServiceBusReceivedMessage) ctor!.Invoke(new object[] { amqpAnnotatedMessage } );
        await OnProcessSessionMessageAsync(new ProcessSessionMessageEventArgs(obj, receiver, token));
    }

    public async Task TriggerExceptionOccured(ProcessErrorEventArgs args)
    {
        await OnProcessErrorAsync(args);
    }

    protected override ServiceBusProcessor InnerProcessor => _innerProcessor;
}
