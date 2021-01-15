﻿using System;
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

        public string Name { get; }

        public ClientType ClientType { get; }

        public Task SendAsync(Message message)
        {
            throw new MessageSenderUnavailableException(Name);
        }

        public Task SendAsync(IList<Message> messageList)
        {
            throw new MessageSenderUnavailableException(Name);
        }

        public Task<long> ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc)
        {
            throw new MessageSenderUnavailableException(Name);
        }

        public Task CancelScheduledMessageAsync(long sequenceNumber)
        {
            throw new MessageSenderUnavailableException(Name);
        }
    }
}
