using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus.Isolation;

public class IsolationService
{
    private readonly IOptions<ServiceBusOptions> _options;
    private readonly ILogger<IsolationService> _logger;
    private readonly ServiceBusRegistry _registry;
    private readonly IMessageMetadataAccessor _messageMetadataAccessor;
    private readonly IsolationSettings _isolationSettings;

    public IsolationService(
        IOptions<ServiceBusOptions> options,
        ILogger<IsolationService> logger,
        ServiceBusRegistry registry,
        IMessageMetadataAccessor messageMetadataAccessor)
    {
        _options = options;
        _logger = logger;
        _registry = registry;
        _messageMetadataAccessor = messageMetadataAccessor;
        _isolationSettings = options.Value.Settings.IsolationSettings;
    }

    public async Task<bool> HandleIsolation(MessageContext context)
    {
        return _isolationSettings.IsolationBehavior switch
        {
            IsolationBehavior.HandleAllMessages => await HandleAllMessages(context),
            IsolationBehavior.HandleIsolatedMessage => await HandleIsolatedMessage(context),
            IsolationBehavior.HandleNonIsolatedMessages => await HandleNonIsolatedMessages(context),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<bool> HandleNonIsolatedMessages(MessageContext context)
    {
        if (context.IsolationApps.Contains(_isolationSettings.ApplicationName) == false)
        {
            return true;
        }
        if (string.IsNullOrEmpty(context.IsolationKey))
        {
            return true;
        }

        _logger.IgnoreMessage("", context.IsolationKey);

        await _messageMetadataAccessor.Metadata!.CompleteMessageAsync();

        await SendToSourceAsync(context, new ServiceBusMessage(context.Message));
        return false;
    }

    private async Task<bool> HandleIsolatedMessage(MessageContext context)
    {
        if (context.IsolationKey == _isolationSettings.IsolationKey
            && context.IsolationApps.Contains(_isolationSettings.ApplicationName))
        {
            return true;
        }

        _logger.IgnoreMessage(_isolationSettings.IsolationKey, context.IsolationKey);

        await _messageMetadataAccessor.Metadata!.CompleteMessageAsync();

        await SendToSourceAsync(context, new ServiceBusMessage(context.Message));
        return false;
    }

    private Task<bool> HandleAllMessages(MessageContext context)
    {
        return Task.FromResult(true);
    }

    private async Task SendToSourceAsync(
        MessageContext messageContext,
        ServiceBusMessage message)
    {
        var senderInfo = GetSenderResourceId(messageContext);

        // Try to get existing sender
        var sender = _registry.TryGetMessageSender(senderInfo.ClientType, senderInfo.ResourceId);
        if (sender != null)
        {
            await sender.SendMessageAsync(message, messageContext.CancellationToken);
            return;
        }

        // Create a temporary sender if no registered sender exists
        var connectionSettings = _options.Value.Settings.ConnectionSettings!;
        var client = _registry.CreateOrGetServiceBusClient(connectionSettings)!;

        await using var tempSender = client.CreateSender(messageContext.ResourceId);
        await tempSender.SendMessageAsync(message, messageContext.CancellationToken);
    }

    private (ClientType ClientType, string ResourceId) GetSenderResourceId(MessageContext messageContext)
    {
        return messageContext.ClientType switch
        {
            ClientType.Subscription =>
                // For subscriptions, ResourceId is in format "topicName/Subscriptions/subscriptionName"
                (ClientType.Topic, messageContext.ResourceId.Split('/')[0]),
            _ => (ClientType.Queue, messageContext.ResourceId)
        };
    }
}
