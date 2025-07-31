using System;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception.Extensions;
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

        var connectionSettings = _options.Value.Settings.ConnectionSettings;

        await context.CompleteAndResendMessageAsync(
            _messageMetadataAccessor,
            _registry,
            connectionSettings!);
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

        var connectionSettings = _options.Value.Settings.ConnectionSettings;

        await context.CompleteAndResendMessageAsync(
            _messageMetadataAccessor,
            _registry,
            connectionSettings!);
        return false;
    }

    private Task<bool> HandleAllMessages(MessageContext context)
    {
        return Task.FromResult(true);
    }
}
