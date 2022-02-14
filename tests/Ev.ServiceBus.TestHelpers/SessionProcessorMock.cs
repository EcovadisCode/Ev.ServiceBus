using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Moq;

namespace Ev.ServiceBus.TestHelpers;

public class SessionProcessorMock : ServiceBusSessionProcessor
{
    public SessionProcessorMock(string queueName, ServiceBusSessionProcessorOptions options)
    {
        ResourceId = queueName;
        Options = options;
    }

    public SessionProcessorMock(string topicName, string subscriptionName, ServiceBusSessionProcessorOptions options)
    {
        ResourceId = $"{topicName}/Subscriptions/{subscriptionName}";
        Options = options;
    }

    public string ResourceId { get; }
    public ServiceBusSessionProcessorOptions Options { get; }

    public async Task TriggerMessageReception(ServiceBusMessage message, CancellationToken token)
    {
        ServiceBusModelFactory.ServiceBusReceivedMessage();
        ServiceBusReceivedMessage obj = (ServiceBusReceivedMessage) typeof(ServiceBusReceivedMessage)
            .GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, Type.EmptyTypes, null)!
            .Invoke(new object[]{message.GetRawAmqpMessage()} );
        await OnProcessSessionMessageAsync(new ProcessSessionMessageEventArgs(obj, new Mock<ServiceBusSessionReceiver>().Object, token));
    }

    public async Task TriggerExceptionOccured(ProcessErrorEventArgs args)
    {
        await OnProcessErrorAsync(args);
    }
}
