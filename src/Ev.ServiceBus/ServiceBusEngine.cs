using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus;

public class ServiceBusEngine
{
    private readonly ILogger<ServiceBusEngine> _logger;
    private readonly IOptions<ServiceBusOptions> _options;
    private readonly IServiceProvider _provider;
    private readonly ServiceBusRegistry _registry;
    private readonly IClientFactory _clientFactory;
    private readonly SortedList<string,ServiceBusClient> _clients;
    private readonly SortedList<string, ReceiverWrapper> _receivers;
    private readonly SortedList<string, ServiceBusSender> _senders;

    public ServiceBusEngine(ILogger<ServiceBusEngine> logger,
        IOptions<ServiceBusOptions> options,
        ServiceBusRegistry registry,
        IClientFactory clientFactory,
        IServiceProvider provider)
    {
        _logger = logger;
        _options = options;
        _registry = registry;
        _clientFactory = clientFactory;
        _provider = provider;
        _clients = new SortedList<string, ServiceBusClient>();
        _receivers = new SortedList<string, ReceiverWrapper>();
        _senders = new SortedList<string, ServiceBusSender>();
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
                CreateOrGetServiceBusClient(_options.Value.Settings.ConnectionSettings);
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

    private ServiceBusClient? CreateOrGetServiceBusClient(ConnectionSettings settings)
    {
        if (_options.Value.Settings.Enabled == false)
        {
            return null;
        }

        if (_clients.TryGetValue(settings.Endpoint, out var client))
        {
            return client;
        }

        client = _clientFactory.Create(settings);
        _clients.Add(settings.Endpoint, client);
        return client;
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
            // we group by connection to have one SenderWrapper by connection
            foreach (var groupByConnection in groupByName.GroupBy(o => o.ConnectionSettings))
            {
                await RegisterReceiver(groupByConnection.ToArray());
            }
        }
    }

    private async Task RegisterReceiver(ReceiverOptions[] receivers)
    {
        var resourceId = receivers.First().ResourceId;
        var clientType = receivers.First().ClientType;
        if (IsReceiverResourceIdTaken(clientType, resourceId))
        {
            resourceId = GetNewReceiverResourceId(clientType, resourceId);
            foreach (var sender in receivers)
            {
                sender.UpdateResourceId(resourceId);
            }
        }

        var receiverOptions = new ComposedReceiverOptions(receivers.Cast<IMessageReceiverOptions>().ToArray());

        try
        {
            var connectionSettings = receiverOptions.ConnectionSettings ?? _options.Value.Settings.ConnectionSettings;
            if (connectionSettings == null)
            {
                throw new MissingConnectionException(receiverOptions.ResourceId, ClientType.Topic);
            }

            var client = CreateOrGetServiceBusClient(connectionSettings);
            var receiverWrapper = new ReceiverWrapper(
                client,
                receiverOptions,
                _options.Value,
                _provider);
            await receiverWrapper.Initialize();
            _receivers.Add(_registry.ComputeResourceKey(receiverOptions.ClientType, receiverOptions.ResourceId), receiverWrapper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Ev.ServiceBus] Initialization of client '{ResourceId}': Failed", receiverOptions.ResourceId);
        }
    }

    private string GetNewReceiverResourceId(ClientType clientType, string resourceId)
    {
        var newResourceId = resourceId;
        var suffix = 2;
        while (IsReceiverResourceIdTaken(clientType, newResourceId))
        {
            newResourceId = $"{resourceId}_{suffix}";
            ++suffix;
        }

        return newResourceId;
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
                RegisterSender(groupByConnection.ToArray());
            }
        }
    }

    private void RegisterSender(ClientOptions[] senderOptions)
    {
        var options = (IClientOptions)senderOptions.First();
        _logger.LogInformation("[Ev.ServiceBus] Initialization of sender client '{ResourceId}': Start", options.ResourceId);
        if (IsSenderResourceIdTaken(options.ClientType, options.ResourceId))
        {
            var resourceId = GetNewSenderResourceId(options.ClientType, options.ResourceId);
            foreach (var sender in senderOptions)
            {
                sender.UpdateResourceId(resourceId);
            }
        }

        if (_options.Value.Settings.Enabled == false)
        {
            _registry.Register(new DeactivatedSender(options.ResourceId, options.ClientType));

            _logger.LogInformation("[Ev.ServiceBus] Initialization of sender client '{ResourceId}': Client deactivated through configuration", options.ResourceId);
            return;
        }

        try
        {
            var connectionSettings = options.ConnectionSettings ?? _options.Value.Settings.ConnectionSettings;
            if (connectionSettings == null)
            {
                throw new MissingConnectionException(senderOptions.First().ResourceId, ClientType.Topic);
            }

            var client = CreateOrGetServiceBusClient(connectionSettings);

            var senderClient = client!.CreateSender(options.ResourceId);
            _senders.Add(_registry.ComputeResourceKey(options.ClientType, options.ResourceId), senderClient);

            var messageSender = new MessageSender(senderClient, options.ResourceId, options.ClientType, _provider.GetRequiredService<ILogger<MessageSender>>());
            _registry.Register(messageSender);

            _logger.LogInformation("[Ev.ServiceBus] Initialization of sender client '{ResourceId}': Success", options.ResourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Ev.ServiceBus] Initialization of sender client '{ResourceId}': Failed", senderOptions.First().ResourceId);
            _registry.Register(new UnavailableSender(options.ResourceId, options.ClientType));
        }
    }

    private string GetNewSenderResourceId(ClientType clientType, string resourceId)
    {
        var newResourceId = resourceId;
        var suffix = 2;
        while (IsSenderResourceIdTaken(clientType, newResourceId))
        {
            newResourceId = $"{resourceId}_{suffix}";
            ++suffix;
        }

        return newResourceId;
    }

    public async Task StopAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Ev.ServiceBus] Stopping azure service bus clients");

        await Task.WhenAll(_senders.Select(async o =>
        {
            var (resourceId, senderClient) = o;
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
                _logger.LogError(ex, "[Ev.ServiceBus] Client {ResourceId} couldn't close properly", resourceId);
            }
        }).ToArray());

        await Task.WhenAll(_receivers.Select(o => o.Value.CloseAsync(cancellationToken)).ToArray());
    }

    private bool IsSenderResourceIdTaken(ClientType clientType, string resourceId)
    {
        return _senders.ContainsKey(_registry.ComputeResourceKey(clientType, resourceId));
    }

    private bool IsReceiverResourceIdTaken(ClientType clientType, string resourceId)
    {
        return _receivers.ContainsKey(_registry.ComputeResourceKey(clientType, resourceId));
    }

    internal ServiceBusSender[] GetAllSenders()
    {
        return _senders.Select(o => o.Value).ToArray();
    }

    internal ReceiverWrapper[] GetAllReceivers()
    {
        return _receivers.Select(o => o.Value).ToArray();
    }
}