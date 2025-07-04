﻿using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Moq;

namespace Ev.ServiceBus.TestHelpers;

public class ProcessorMock : ServiceBusProcessor
{
    private bool _closed;

    public ProcessorMock(string queueName, ServiceBusProcessorOptions options)
    {
        ResourceId = queueName;
        Options = options;
    }

    public ProcessorMock(string topicName, string subscriptionName, ServiceBusProcessorOptions options)
    {
        ResourceId = $"{topicName}/Subscriptions/{subscriptionName}";
        Options = options;
    }

    public string ResourceId { get; }
    public ServiceBusProcessorOptions Options { get; }

    public Task TriggerMessageReception(ServiceBusMessage message, CancellationToken token)
    {
        return TriggerMessageReception(message, token, new Mock<ServiceBusReceiver>().Object);
    }

    public async Task TriggerMessageReception(ServiceBusMessage message, CancellationToken token, ServiceBusReceiver receiver)
    {
        var amqpAnnotatedMessage = message.GetRawAmqpMessage();
        amqpAnnotatedMessage.Header.DeliveryCount = 1;
        ServiceBusReceivedMessage obj = (ServiceBusReceivedMessage) typeof(ServiceBusReceivedMessage)
            .GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, new []{typeof(AmqpAnnotatedMessage)}, null)!
            .Invoke(new object[] { amqpAnnotatedMessage });
        await OnProcessMessageAsync(new ProcessMessageEventArgs(obj, receiver, token));
        // protected internal virtual async Task OnProcessMessageAsync(ProcessMessageEventArgs args)
        // var method =typeof(ServiceBusProcessor).GetMethod("OnProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[]{typeof(ProcessMessageEventArgs)}, null);
        // method.Invoke(Mock.Object, new []{new ProcessMessageEventArgs(obj, new Mock<ServiceBusReceiver>().Object, token)});
        // Mock.Raise(m => m.ProcessMessageAsync += null, new ProcessMessageEventArgs(obj, new Mock<ServiceBusReceiver>().Object, token));
    }

    public async Task TriggerExceptionOccured(ProcessErrorEventArgs args)
    {
        await OnProcessErrorAsync(args);
    }

    public override Task StartProcessingAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public override Task StopProcessingAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public override Task CloseAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        _closed = true;
        return Task.CompletedTask;
    }

    public override bool IsClosed => _closed;
}
