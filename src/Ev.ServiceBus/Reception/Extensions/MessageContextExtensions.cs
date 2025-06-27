using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Management;

namespace Ev.ServiceBus.Reception.Extensions;

public static class MessageContextExtensions
{
    public static async Task CompleteAndResendMessageAsync(this MessageContext messageContext,
        IMessageMetadataAccessor messageMetadataAccessor,
        ServiceBusRegistry registry,
        ConnectionSettings connectionSettings)
    {
        await messageMetadataAccessor.Metadata!.CompleteMessageAsync();

        await SendToSourceAsync(messageContext, new ServiceBusMessage(messageContext.Message),
            registry, connectionSettings, messageContext.CancellationToken);
    }

    private static async Task SendToSourceAsync(this MessageContext messageContext,
        ServiceBusMessage message,
        ServiceBusRegistry registry,
        ConnectionSettings connectionSettings,
        CancellationToken cancellationToken)
    {
        if (messageContext == null) throw new ArgumentNullException(nameof(messageContext));
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (registry == null) throw new ArgumentNullException(nameof(registry));

        switch (messageContext.ClientType)
        {
            case ClientType.Queue:
            {
                // Try to get existing sender
                var sender = registry.TryGetMessageSender(messageContext.ClientType, messageContext.ResourceId);
                if (sender != null)
                {
                    await sender.SendMessageAsync(message, cancellationToken);
                    return;
                }

                // Create a temporary sender if no registered sender exists
                var client = registry.CreateOrGetServiceBusClient(connectionSettings)
                             ?? throw new InvalidOperationException("Failed to create ServiceBusClient");

                await using var tempSender = client.CreateSender(messageContext.ResourceId);
                await tempSender.SendMessageAsync(message, cancellationToken);
                break;
            }
            case ClientType.Subscription:
            {
                // For subscriptions, ResourceId is in format "topicName/Subscriptions/subscriptionName"
                var topicName = messageContext.ResourceId.Split('/')[0];

                // Try to get existing sender for the topic
                var sender = registry.TryGetMessageSender(ClientType.Topic, topicName);
                if (sender != null)
                {
                    await sender.SendMessageAsync(message, cancellationToken);
                    return;
                }

                // Create a temporary sender if no registered sender exists
                var client = registry.CreateOrGetServiceBusClient(connectionSettings)
                             ?? throw new InvalidOperationException("Failed to create ServiceBusClient");

                await using var tempSender = client.CreateSender(topicName);
                await tempSender.SendMessageAsync(message, cancellationToken);
                break;
            }
            default:
                throw new ArgumentException($"Unsupported client type: {messageContext.ClientType}", nameof(messageContext));
        }
    }
}