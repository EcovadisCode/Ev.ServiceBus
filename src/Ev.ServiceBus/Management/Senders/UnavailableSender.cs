using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;

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
        public Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default) { throw new MessageSenderUnavailableException(Name); }
        /// <inheritdoc />
        public Task SendMessagesAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default) { throw new MessageSenderUnavailableException(Name); }
        /// <inheritdoc />
        public ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CancellationToken cancellationToken = default) { throw new MessageSenderUnavailableException(Name); }
        /// <inheritdoc />
        public ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CreateMessageBatchOptions options, CancellationToken cancellationToken = default) { throw new MessageSenderUnavailableException(Name); }
        /// <inheritdoc />
        public Task SendMessagesAsync(ServiceBusMessageBatch messageBatch, CancellationToken cancellationToken = default) { throw new MessageSenderUnavailableException(Name); }

        /// <inheritdoc />
        public Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime,
            CancellationToken cancellationToken = default)
        {
            throw new MessageSenderUnavailableException(Name);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<long>> ScheduleMessagesAsync(IEnumerable<ServiceBusMessage> messages, DateTimeOffset scheduledEnqueueTime,
            CancellationToken cancellationToken = default)
        {
            throw new MessageSenderUnavailableException(Name);
        }

        /// <inheritdoc />
        public Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default) { throw new MessageSenderUnavailableException(Name); }
        /// <inheritdoc />
        public Task CancelScheduledMessagesAsync(IEnumerable<long> sequenceNumbers, CancellationToken cancellationToken = default) { throw new MessageSenderUnavailableException(Name); }
    }
}
