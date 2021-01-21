using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using IMessageSender = Ev.ServiceBus.Abstractions.IMessageSender;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class MessageSender : IMessageSender
    {
        private readonly ISenderClient _client;

        public MessageSender(ISenderClient client, string name, ClientType clientType)
        {
            _client = client;
            Name = name;
            ClientType = clientType;
        }

        public string Name { get; }
        public ClientType ClientType { get; }

        public Task SendAsync(Message message)
        {
            return _client.SendAsync(message);
        }

        public Task SendAsync(IList<Message> messageList)
        {
            return _client.SendAsync(messageList);
        }

        public Task<long> ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc)
        {
            return _client.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);
        }

        public Task CancelScheduledMessageAsync(long sequenceNumber)
        {
            return _client.CancelScheduledMessageAsync(sequenceNumber);
        }
    }
}
