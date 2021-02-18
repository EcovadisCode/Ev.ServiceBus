using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus
{
    public class ServiceBusRegistry : IServiceBusRegistry
    {
        private readonly SortedList<string, ReceiverWrapper> _receivers;
        private readonly SortedList<string, SenderWrapper> _senders;

        public ServiceBusRegistry()
        {
            _senders = new SortedList<string, SenderWrapper>();
            _receivers = new SortedList<string, ReceiverWrapper>();
        }

        public IMessageSender GetQueueSender(string name)
        {
            if (_senders.TryGetValue(ComputeSenderKey(ClientType.Queue, name), out var queue))
            {
                return queue.Sender;
            }

            throw new QueueSenderNotFoundException(name);
        }

        public IMessageSender GetTopicSender(string name)
        {
            if (_senders.TryGetValue(ComputeSenderKey(ClientType.Topic, name), out var topic))
            {
                return topic.Sender;
            }

            throw new TopicSenderNotFoundException(name);
        }

        private string ComputeSenderKey(ClientType clientType, string resourceId)
        {
            return $"{clientType}|{resourceId}";
        }

        public void Register(SenderWrapper senderWrapper)
        {
            _senders.Add(ComputeSenderKey(senderWrapper.ClientType, senderWrapper.ResourceId), senderWrapper);
        }

        public void Register(ReceiverWrapper receiverWrapper)
        {
            _receivers.Add(ComputeSenderKey(receiverWrapper.ClientType, receiverWrapper.ResourceId), receiverWrapper);
        }

        internal bool IsSenderResourceIdTaken(ClientType clientType, string resourceId)
        {
            return _senders.ContainsKey(ComputeSenderKey(clientType, resourceId));
        }

        internal bool IsReceiverResourceIdTaken(ClientType clientType, string resourceId)
        {
            return _receivers.ContainsKey(ComputeSenderKey(clientType, resourceId));
        }

        internal SenderWrapper[] GetAllSenders()
        {
            return _senders.Values.ToArray();
        }

        internal ReceiverWrapper[] GetAllReceivers()
        {
            return _receivers.Values.ToArray();
        }
    }
}
