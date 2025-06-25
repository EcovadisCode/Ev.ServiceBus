using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Reception.Extensions;

public static class MessageContextExtensions
{
    public static async Task CompleteAndResendMessageAsync(this MessageContext messageContext,
        IMessageMetadataAccessor messageMetadataAccessor,
        ServiceBusClient client)
    {
        await messageMetadataAccessor.Metadata!.CompleteMessageAsync();

        await SendToSourceAsync(messageContext, new ServiceBusMessage(messageContext.Message),
            client, messageContext.CancellationToken);
    }

    private static async Task SendToSourceAsync(this MessageContext messageContext,
        ServiceBusMessage message,
        ServiceBusClient client,
        CancellationToken cancellationToken)
    {
        if (messageContext == null) throw new ArgumentNullException(nameof(messageContext));
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (client == null) throw new ArgumentNullException(nameof(client));

        switch (messageContext.ClientType)
        {
            case ClientType.Queue:
            {
                // For queues, ResourceId is directly the queue name
                await using var sender = client.CreateSender(messageContext.ResourceId);
                await sender.SendMessageAsync(message, cancellationToken);
                break;
            }
            case ClientType.Subscription:
            {
                // For subscriptions, ResourceId is in format "topicName/Subscriptions/subscriptionName"
                var topicName = messageContext.ResourceId.Split('/')[0];
                await using var sender = client.CreateSender(topicName);
                await sender.SendMessageAsync(message, cancellationToken);
                break;
            }
            default:
                throw new ArgumentException($"Unsupported client type: {messageContext.ClientType}", nameof(messageContext));
        }
    }
}