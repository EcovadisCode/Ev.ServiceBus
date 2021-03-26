using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus
{
    public class DeactivatedSender : IMessageSender
    {
        public DeactivatedSender(string name, ClientType queue)
        {
            Name = name;
            ClientType = queue;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public ClientType ClientType { get; }

        /// <inheritdoc />
        public Task SendAsync(Message message)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SendAsync(IList<Message> messageList)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<long> ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc)
        {
            return Task.FromResult((long)1);
        }

        /// <inheritdoc />
        public Task CancelScheduledMessageAsync(long sequenceNumber)
        {
            return Task.CompletedTask;
        }
    }
}
