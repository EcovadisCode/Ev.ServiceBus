using System;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus;

public class MessageSenderFactory
{
    private readonly ILogger<LoggingExtensions.ServiceBusClientManagement> _logger;
    private readonly IOptions<ServiceBusOptions> _options;
    private readonly ServiceBusRegistry _registry;
    private readonly IServiceProvider _provider;

    public MessageSenderFactory(
        ILogger<LoggingExtensions.ServiceBusClientManagement> logger,
        IOptions<ServiceBusOptions> options,
        ServiceBusRegistry registry,
        IServiceProvider provider)
    {
        _logger = logger;
        _options = options;
        _registry = registry;
        _provider = provider;
    }

    public IMessageSender CreateSender(ClientOptions[] senderOptions)
    {
        var options = (IClientOptions)senderOptions.First();
        if (_registry.IsSenderResourceIdTaken(options.ClientType, options.ResourceId))
        {
            var resourceId = GetNewSenderResourceId(options.ClientType, options.ResourceId);
            foreach (var sender in senderOptions)
            {
                sender.UpdateResourceId(resourceId);
            }
        }

        if (_options.Value.Settings.Enabled == false)
        {
            _logger.SenderClientDeactivatedThroughConfiguration(options.ResourceId);
            return new DeactivatedSender(options.ResourceId, options.ClientType);
        }

        try
        {
            var connectionSettings = options.ConnectionSettings ?? _options.Value.Settings.ConnectionSettings;
            if (connectionSettings == null)
            {
                throw new MissingConnectionException(senderOptions.First().ResourceId, ClientType.Topic);
            }

            var client = _registry.CreateOrGetServiceBusClient(connectionSettings);

            var senderClient = client!.CreateSender(options.ResourceId);
            _registry.Register(options.ClientType, options.ResourceId, senderClient);

            var messageSender = new MessageSender(senderClient, options.ResourceId, options.ClientType, _provider.GetRequiredService<ILogger<MessageSender>>());

            _logger.SenderClientInitialized(options.ResourceId);
            return messageSender;
        }
        catch (Exception ex)
        {
            _logger.SenderClientFailedToInitialize(senderOptions.First().ResourceId, ex);
            return new UnavailableSender(options.ResourceId, options.ClientType);
        }
    }

    private string GetNewSenderResourceId(ClientType clientType, string resourceId)
    {
        var newResourceId = resourceId;
        var suffix = 2;
        while (_registry.IsSenderResourceIdTaken(clientType, newResourceId))
        {
            newResourceId = $"{resourceId}_{suffix}";
            ++suffix;
        }

        return newResourceId;
    }
}
