using System;
using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Management
{
    public class ServiceBusRegistry : IServiceBusRegistry
    {
        private readonly SortedList<string, ReceiverWrapper> _receivers;
        private readonly SortedList<string, SenderWrapper> _senders;
        private readonly Dictionary<string, MessageReceptionRegistration> _receptions;
        private readonly Dictionary<Type, MessageDispatchRegistration[]> _dispatches;

        public ServiceBusRegistry()
        {
            _senders = new SortedList<string, SenderWrapper>();
            _receivers = new SortedList<string, ReceiverWrapper>();
            _receptions = new Dictionary<string, MessageReceptionRegistration>();
            _dispatches = new Dictionary<Type, MessageDispatchRegistration[]>();
        }

        public IMessageSender GetQueueSender(string name)
        {
            if (_senders.TryGetValue(ComputeResourceKey(ClientType.Queue, name), out var queue))
            {
                return queue.Sender;
            }

            throw new QueueSenderNotFoundException(name);
        }

        public IMessageSender GetTopicSender(string name)
        {
            if (_senders.TryGetValue(ComputeResourceKey(ClientType.Topic, name), out var topic))
            {
                return topic.Sender;
            }

            throw new TopicSenderNotFoundException(name);
        }

        private string ComputeResourceKey(ClientType clientType, string resourceId)
        {
            return $"{clientType}|{resourceId}";
        }

        private string ComputeReceptionKey(string payloadTypeId, string receiverName, ClientType clientType)
        {
            return $"{clientType}|{receiverName}|{payloadTypeId}";
        }

        internal void Register(SenderWrapper senderWrapper)
        {
            _senders.Add(ComputeResourceKey(senderWrapper.ClientType, senderWrapper.ResourceId), senderWrapper);
        }

        internal void Register(ReceiverWrapper receiverWrapper)
        {
            _receivers.Add(ComputeResourceKey(receiverWrapper.ClientType, receiverWrapper.ResourceId), receiverWrapper);
        }

        internal void Register(MessageReceptionRegistration reception)
        {
            _receptions.Add(ComputeReceptionKey(reception.PayloadTypeId, reception.Options.ResourceId, reception.Options.ClientType), reception);
        }

        internal void Register(Type dispatchType, MessageDispatchRegistration[] dispatches)
        {
            _dispatches.Add(dispatchType, dispatches);
        }

        internal bool IsSenderResourceIdTaken(ClientType clientType, string resourceId)
        {
            return _senders.ContainsKey(ComputeResourceKey(clientType, resourceId));
        }

        internal bool IsReceiverResourceIdTaken(ClientType clientType, string resourceId)
        {
            return _receivers.ContainsKey(ComputeResourceKey(clientType, resourceId));
        }

        internal SenderWrapper[] GetAllSenders()
        {
            return _senders.Values.ToArray();
        }

        internal ReceiverWrapper[] GetAllReceivers()
        {
            return _receivers.Values.ToArray();
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
