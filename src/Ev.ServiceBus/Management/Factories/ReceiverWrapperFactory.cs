using System;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus;

public class ReceiverWrapperFactory
{
    private readonly ILogger<ReceiverWrapperFactory> _logger;
    private readonly IOptions<ServiceBusOptions> _options;
    private readonly ServiceBusRegistry _registry;
    private readonly IServiceProvider _provider;

    public ReceiverWrapperFactory(
        ILogger<ReceiverWrapperFactory> logger,
        IOptions<ServiceBusOptions> options,
        ServiceBusRegistry registry,
        IServiceProvider provider)
    {
        _logger = logger;
        _options = options;
        _registry = registry;
        _provider = provider;
    }

    public async Task<ReceiverWrapper?> CreateReceiver(ComposedReceiverOptions receiverOptions)
    {
        var resourceId = receiverOptions.ResourceId;
        var clientType = receiverOptions.ClientType;
        if (_registry.IsReceiverResourceIdTaken(clientType, resourceId))
        {
            resourceId = GetNewReceiverResourceId(clientType, resourceId);
            receiverOptions.UpdateResourceId(resourceId);
        }

        try
        {
            var connectionSettings = receiverOptions.ConnectionSettings ?? _options.Value.Settings.ConnectionSettings;
            if (connectionSettings == null)
            {
                throw new MissingConnectionException(receiverOptions.ResourceId, ClientType.Topic);
            }

            var client = _registry.CreateOrGetServiceBusClient(connectionSettings);
            ReceiverWrapper receiverWrapper;
            if (receiverOptions.SessionMode)
            {
                receiverWrapper = new SessionReceiverWrapper(
                    client,
                    receiverOptions,
                    _options.Value,
                    _provider);
            }
            else
            {
                receiverWrapper = new ReceiverWrapper(
                    client,
                    receiverOptions,
                    _options.Value,
                    _provider);
            }
            await receiverWrapper.Initialize();
            return receiverWrapper;
        }
        catch (Exception ex)
        {
            _logger.ReceiverClientFailedToInitialize(receiverOptions.ResourceId, ex);
        }

        return null;
    }

    private string GetNewReceiverResourceId(ClientType clientType, string resourceId)
    {
        var newResourceId = resourceId;
        var suffix = 2;
        while (_registry.IsReceiverResourceIdTaken(clientType, newResourceId))
        {
            newResourceId = $"{resourceId}_{suffix}";
            ++suffix;
        }

        return newResourceId;
    }
}
