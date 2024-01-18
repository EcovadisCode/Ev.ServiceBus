using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus;

public class ServiceBusEngine
{
    private readonly ILogger<ServiceBusEngine> _logger;
    private readonly IOptions<ServiceBusOptions> _options;
    private readonly ServiceBusRegistry _registry;
    private readonly MessageSenderFactory _messageSenderFactory;
    private readonly ReceiverWrapperFactory _receiverWrapperFactory;

    public ServiceBusEngine(
        ILogger<ServiceBusEngine> logger,
        IOptions<ServiceBusOptions> options,
        ServiceBusRegistry registry,
        MessageSenderFactory messageSenderFactory,
        ReceiverWrapperFactory receiverWrapperFactory)
    {
        _logger = logger;
        _options = options;
        _registry = registry;
        _messageSenderFactory = messageSenderFactory;
        _receiverWrapperFactory = receiverWrapperFactory;
    }

    public async Task StartAll()
    {
        _logger.LogInformation("[Ev.ServiceBus] Starting azure service bus clients");

        if (_options.Value.Settings.Enabled == false)
        {
            _logger.LogInformation("[Ev.ServiceBus] Reception and dispatch of messages have been deactivated through configuration.");
        }
        if (_options.Value.Settings.Enabled && _options.Value.Settings.ReceiveMessages == false)
        {
            _logger.LogInformation("[Ev.ServiceBus] Reception of messages have been deactivated through configuration.");
        }

        if (_options.Value.Settings.ConnectionSettings != null)
        {
            try
            {
                _registry.CreateOrGetServiceBusClient(_options.Value.Settings.ConnectionSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initialization of common connection failed");
            }
        }

        BuildSenders();
        await BuildReceivers();
        BuildReceptions();
        BuildDispatches();
    }

    private void BuildDispatches()
    {
        var doubleRegistrations = _options.Value.DispatchRegistrations.GroupBy(o => o).Where(o => o.Count() > 1).ToArray();
        if (doubleRegistrations.Any())
        {
            throw new MultiplePublicationRegistrationException(doubleRegistrations.Select(o => o.Key.ToString()).ToArray());
        }

        foreach (var group in _options.Value.DispatchRegistrations.GroupBy(o => o.PayloadType))
        {
            _registry.Register(group.Key, group.ToArray());
        }
    }

    private void BuildReceptions()
    {
        var regs = _options.Value.ReceptionRegistrations.ToArray();

        var duplicatedHandlers = regs.GroupBy(o => new { o.Options.ClientType,
            o.Options.ResourceId, o.HandlerType }).Where(o => o.Count() > 1).ToArray();
        if (duplicatedHandlers.Any())
        {
            throw new DuplicateSubscriptionHandlerDeclarationException(duplicatedHandlers.SelectMany(o => o).ToArray());
        }

        var duplicateEvenTypeIds = regs.GroupBy(o => new {o.Options.ClientType,
            o.Options.ResourceId, o.PayloadTypeId}).Where(o => o.Count() > 1).ToArray();
        if (duplicateEvenTypeIds.Any())
        {
            throw new DuplicateEvenTypeIdDeclarationException(duplicateEvenTypeIds.SelectMany(o => o).ToArray());
        }

        foreach (var registration in regs)
        {
            _registry.Register(registration);
        }
    }

    private async Task BuildReceivers()
    {
        var receiversByName = _options.Value.Receivers.GroupBy(o => new { o.ResourceId, o.ClientType }).ToArray();
        foreach (var groupByName in receiversByName)
        {
            // we group by connection to have one ReceiverWrapper by connection
            foreach (var groupByConnection in groupByName.GroupBy(o => o.ConnectionSettings))
            {
                var receiverOptions = new ComposedReceiverOptions(groupByConnection.ToArray());
                var receiverWrapper = await _receiverWrapperFactory.CreateReceiver(receiverOptions);
                if (receiverWrapper != null)
                {
                    _registry.Register(receiverOptions.ClientType, receiverOptions.ResourceId, receiverWrapper);
                }
            }
        }
    }

    private void BuildSenders()
    {
        var senders = _options.Value.Senders;

        var sendersByName = senders.GroupBy(o => new { o.ResourceId, o.ClientType }).ToArray();
        foreach (var groupByName in sendersByName)
        {
            // we group by connection to have one SenderWrapper by connection
            foreach (var groupByConnection in groupByName.GroupBy(o => o.ConnectionSettings))
            {
                var messageSender = _messageSenderFactory.CreateSender(groupByConnection.ToArray());
                _registry.Register(messageSender);
            }
        }
    }

    public async Task StopAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Ev.ServiceBus] Stopping azure service bus clients");

        await Task.WhenAll(_registry.GetAllSenderClients().Select(async senderClient =>
        {
            if (senderClient.IsClosed == true)
            {
                return;
            }

            try
            {
                await senderClient.CloseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Ev.ServiceBus] Client {ResourceId} couldn't close properly", senderClient.EntityPath);
            }
        }).ToArray());

        await Task.WhenAll(_registry.GetAllReceivers().Select(o => o.CloseAsync(cancellationToken)).ToArray());
    }

}