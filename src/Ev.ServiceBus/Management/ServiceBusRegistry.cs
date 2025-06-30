using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus.Management;

public class ServiceBusRegistry : IServiceBusRegistry
{
    private readonly IClientFactory _clientFactory;
    private readonly IOptions<ServiceBusOptions> _options;
    private readonly SortedList<string, ServiceBusClient> _clients;
    private readonly SortedList<string, ReceiverWrapper> _receivers;
    private readonly SortedList<string, ServiceBusSender> _senderClients;

    private readonly SortedList<string, IMessageSender> _messageSenders;
    private readonly Dictionary<string, MessageReceptionRegistration> _receptions;
    private readonly Dictionary<Type, MessageDispatchRegistration[]> _dispatches;

    public ServiceBusRegistry(
        IClientFactory clientFactory,
        IOptions<ServiceBusOptions> options)
    {
        _clientFactory = clientFactory;
        _options = options;
        _clients = new SortedList<string, ServiceBusClient>();
        _receivers = new SortedList<string, ReceiverWrapper>();
        _senderClients = new SortedList<string, ServiceBusSender>();
        _messageSenders = new SortedList<string, IMessageSender>();
        _receptions = new Dictionary<string, MessageReceptionRegistration>();
        _dispatches = new Dictionary<Type, MessageDispatchRegistration[]>();
    }

    public ServiceBusClient? CreateOrGetServiceBusClient(ConnectionSettings settings)
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

    public IMessageSender? TryGetMessageSender(ClientType clientType, string resourceId)
    {
        return _messageSenders.GetValueOrDefault(ComputeResourceKey(clientType, resourceId));
    }

    public IMessageSender GetMessageSender(ClientType clientType, string resourceId)
    {
        if (_messageSenders.TryGetValue(ComputeResourceKey(clientType, resourceId), out var sender))
        {
            return sender;
        }

        throw new SenderNotFoundException(clientType, resourceId);
    }

    internal ServiceBusSender[] GetAllSenderClients()
    {
        return _senderClients.Select(o => o.Value).ToArray();
    }

    internal ReceiverWrapper[] GetAllReceivers()
    {
        return _receivers.Select(o => o.Value).ToArray();
    }

    internal string ComputeResourceKey(ClientType clientType, string resourceId)
    {
        return $"{clientType}|{resourceId}";
    }

    private string ComputeReceptionKey(string payloadTypeId, string receiverName, ClientType clientType)
    {
        return $"{clientType}|{receiverName}|{payloadTypeId.ToLower()}";
    }

    internal void Register(IMessageSender sender)
    {
        _messageSenders.Add(ComputeResourceKey(sender.ClientType, sender.Name), sender);
    }

    internal void Register(MessageReceptionRegistration reception)
    {
        _receptions.Add(ComputeReceptionKey(reception.PayloadTypeId, reception.Options.ResourceId, reception.Options.ClientType), reception);
    }

    internal void Register(Type dispatchType, MessageDispatchRegistration[] dispatches)
    {
        _dispatches.Add(dispatchType, dispatches);
    }

    public void Register(ClientType clientType, string resourceId, ServiceBusSender senderClient)
    {
        _senderClients.Add(ComputeResourceKey(clientType, resourceId), senderClient);
    }

    public void Register(ClientType clientType, string resourceId, ReceiverWrapper receiverWrapper)
    {
        _receivers.Add(ComputeResourceKey(clientType, resourceId), receiverWrapper);
    }

    /// <inheritdoc />
    public MessageReceptionRegistration? GetReceptionRegistration(string payloadTypeId, string receiverName, ClientType clientType)
    {
        if (_receptions.TryGetValue(ComputeReceptionKey(payloadTypeId, receiverName, clientType), out var registrations))
        {
            return registrations;
        }

        return null;
    }

    /// <inheritdoc />
    public MessageDispatchRegistration[] GetDispatchRegistrations(Type messageType)
    {
        if (_dispatches.TryGetValue(messageType, out var registrations))
        {
            return registrations;
        }

        throw new DispatchRegistrationNotFoundException(messageType);
    }

    public MessageReceptionRegistration[] GetReceptionRegistrations()
    {
        return _receptions.Values.ToArray();
    }

    internal bool IsSenderResourceIdTaken(ClientType clientType, string resourceId)
    {
        return _senderClients.ContainsKey(ComputeResourceKey(clientType, resourceId));
    }

    internal bool IsReceiverResourceIdTaken(ClientType clientType, string resourceId)
    {
        return _receivers.ContainsKey(ComputeResourceKey(clientType, resourceId));
    }

}
