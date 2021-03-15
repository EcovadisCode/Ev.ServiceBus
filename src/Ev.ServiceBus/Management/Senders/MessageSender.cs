using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using IMessageSender = Ev.ServiceBus.Abstractions.IMessageSender;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class MessageSender : IMessageSender
    {
        private readonly ISenderClient _client;
        private readonly ILogger<MessageSender> _logger;

        public MessageSender(ISenderClient client, string name, ClientType clientType, ILogger<MessageSender> logger)
        {
            _client = client;
            _logger = logger;
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
            _logger.LogInformation($"[Ev.ServiceBus] Sending a message to {ClientType} {Name}");
            return _client.SendAsync(message);
        }

        /// <inheritdoc />
        public Task SendAsync(IList<Message> messageList)
        {
            _logger.LogInformation($"[Ev.ServiceBus] Sending {messageList.Count()} messages to {ClientType} {Name}");
            return _client.SendAsync(messageList);
        }

        /// <inheritdoc />
        public Task<long> ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc)
        {
            _logger.LogInformation($"[Ev.ServiceBus] Scheduling a message to {ClientType} {Name} to be executed at {scheduleEnqueueTimeUtc:O}");
            return _client.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);
        }

        /// <inheritdoc />
        public Task CancelScheduledMessageAsync(long sequenceNumber)
        {
            _logger.LogInformation($"[Ev.ServiceBus] Cancelling a scheduled message {ClientType} {Name}");
            return _client.CancelScheduledMessageAsync(sequenceNumber);
        }
    }
}
