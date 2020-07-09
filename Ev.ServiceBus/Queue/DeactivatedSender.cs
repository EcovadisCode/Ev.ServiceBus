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

        public string Name { get; }
        public ClientType ClientType { get; }

        public Task SendAsync(Message message)
        {
            return Task.CompletedTask;
        }

        public Task SendAsync(IList<Message> messageList)
        {
            return Task.CompletedTask;
        }

        public Task<long> ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc)
        {
            return Task.FromResult((long)1);
        }

        public Task CancelScheduledMessageAsync(long sequenceNumber)
        {
            return Task.CompletedTask;
        }
    }
}
