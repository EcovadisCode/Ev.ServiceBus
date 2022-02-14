using System;
using System.Collections.Generic;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Management
{
    public class ServiceBusRegistry : IServiceBusRegistry
    {
        private readonly SortedList<string, IMessageSender> _senders;
        private readonly Dictionary<string, MessageReceptionRegistration> _receptions;
        private readonly Dictionary<Type, MessageDispatchRegistration[]> _dispatches;

        public ServiceBusRegistry()
        {
            _senders = new SortedList<string, IMessageSender>();
            _receptions = new Dictionary<string, MessageReceptionRegistration>();
            _dispatches = new Dictionary<Type, MessageDispatchRegistration[]>();
        }

        public IMessageSender GetQueueSender(string name)
        {
            if (_senders.TryGetValue(ComputeResourceKey(ClientType.Queue, name), out var sender))
            {
                return sender;
            }

            throw new QueueSenderNotFoundException(name);
        }

        public IMessageSender GetTopicSender(string name)
        {
            if (_senders.TryGetValue(ComputeResourceKey(ClientType.Topic, name), out var sender))
            {
                return sender;
            }

            throw new TopicSenderNotFoundException(name);
        }

        internal string ComputeResourceKey(ClientType clientType, string resourceId)
        {
            return $"{clientType}|{resourceId}";
        }

        private string ComputeReceptionKey(string payloadTypeId, string receiverName, ClientType clientType)
        {
            return $"{clientType}|{receiverName}|{payloadTypeId}";
        }

        internal void Register(IMessageSender sender)
        {
            _senders.Add(ComputeResourceKey(sender.ClientType, sender.Name), sender);
        }

        internal void Register(MessageReceptionRegistration reception)
        {
            _receptions.Add(ComputeReceptionKey(reception.PayloadTypeId, reception.Options.ResourceId, reception.Options.ClientType), reception);
        }

        internal void Register(Type dispatchType, MessageDispatchRegistration[] dispatches)
        {
            _dispatches.Add(dispatchType, dispatches);
        }

        internal MessageReceptionRegistration? GetReceptionRegistration(string payloadTypeId, string receiverName, ClientType clientType)
        {
            if (_receptions.TryGetValue(ComputeReceptionKey(payloadTypeId, receiverName, clientType), out var registrations))
            {
                return registrations;
            }

            return null;
        }

        internal MessageDispatchRegistration[] GetDispatchRegistrations(Type messageType)
        {
            if (_dispatches.TryGetValue(messageType, out var registrations))
            {
                return registrations;
            }

            throw new DispatchRegistrationNotFoundException(messageType);
        }
    }
}
