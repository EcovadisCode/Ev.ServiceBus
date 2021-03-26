using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus
{
    public class UnavailableSender : IMessageSender
    {
        public UnavailableSender(string name, ClientType clientType)
        {
            Name = name;
            ClientType = clientType;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public ClientType ClientType { get; }

        /// <inheritdoc />
        public Task SendAsync(Message message)
        {
            throw new MessageSenderUnavailableException(Name);
        }

        /// <inheritdoc />
        public Task SendAsync(IList<Message> messageList)
        {
            throw new MessageSenderUnavailableException(Name);
        }

        /// <inheritdoc />
        public Task<long> ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc)
        {
            throw new MessageSenderUnavailableException(Name);
        }

        /// <inheritdoc />
        public Task CancelScheduledMessageAsync(long sequenceNumber)
        {
            throw new MessageSenderUnavailableException(Name);
        }
    }
}
