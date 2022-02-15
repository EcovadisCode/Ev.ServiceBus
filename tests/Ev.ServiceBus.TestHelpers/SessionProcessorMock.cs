using System;
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

    public async Task TriggerMessageReception(ServiceBusMessage message, CancellationToken token)
    {
        ServiceBusModelFactory.ServiceBusReceivedMessage();
        var ctor = typeof(ServiceBusReceivedMessage).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
            null, new []{typeof(AmqpAnnotatedMessage)}, null);
        var obj = (ServiceBusReceivedMessage) ctor!.Invoke(new object[]{message.GetRawAmqpMessage()} );
        await OnProcessSessionMessageAsync(new ProcessSessionMessageEventArgs(obj, new Mock<ServiceBusSessionReceiver>().Object, token));
    }

    public async Task TriggerExceptionOccured(ProcessErrorEventArgs args)
    {
        await OnProcessErrorAsync(args);
    }

    protected override ServiceBusProcessor InnerProcessor => _innerProcessor;
}
