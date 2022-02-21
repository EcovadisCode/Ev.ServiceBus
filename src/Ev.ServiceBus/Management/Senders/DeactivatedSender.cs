using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus;

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
    public Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendMessagesAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CancellationToken cancellationToken = default) { return new ValueTask<ServiceBusMessageBatch>(); }
    /// <inheritdoc />
    public ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CreateMessageBatchOptions options, CancellationToken cancellationToken = default) { return new ValueTask<ServiceBusMessageBatch>(); }

    /// <inheritdoc />
    public Task SendMessagesAsync(ServiceBusMessageBatch messageBatch, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)1);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<long>> ScheduleMessagesAsync(IEnumerable<ServiceBusMessage> messages, DateTimeOffset scheduledEnqueueTime,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<long>>(new []{(long)1});
    }

    /// <inheritdoc />
    public Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelScheduledMessagesAsync(IEnumerable<long> sequenceNumbers, CancellationToken cancellationToken = default) { return Task.CompletedTask; }
}