using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.Logging;
using IMessageSender = Ev.ServiceBus.Abstractions.IMessageSender;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus;

public class MessageSender : IMessageSender
{
    private readonly ServiceBusSender _client;
    private readonly ILogger<MessageSender> _logger;

    public MessageSender(ServiceBusSender client, string name, ClientType clientType, ILogger<MessageSender> logger)
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
    public async Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        await _client.SendMessageAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendMessagesAsync(IEnumerable<ServiceBusMessage> messages,
        CancellationToken cancellationToken = default)
    {
        await _client.SendMessagesAsync(messages, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(
        CancellationToken cancellationToken = default)
    {
        return await _client.CreateMessageBatchAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CreateMessageBatchOptions options,
        CancellationToken cancellationToken = default)
    {
        return await _client.CreateMessageBatchAsync(options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendMessagesAsync(ServiceBusMessageBatch messageBatch,
        CancellationToken cancellationToken = default)
    {
        await _client.SendMessagesAsync(messageBatch, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default)
    {
        return await _client.ScheduleMessageAsync(message, scheduledEnqueueTime, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> ScheduleMessagesAsync(IEnumerable<ServiceBusMessage> messages, DateTimeOffset scheduledEnqueueTime,
        CancellationToken cancellationToken = default)
    {
        return await _client.ScheduleMessagesAsync(messages, scheduledEnqueueTime, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default)
    {
        await _client.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelScheduledMessagesAsync(IEnumerable<long> sequenceNumbers,
        CancellationToken cancellationToken = default)
    {
        await _client.CancelScheduledMessagesAsync(sequenceNumbers, cancellationToken);
    }
}