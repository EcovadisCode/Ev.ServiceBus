using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions.MessageReception;

public interface IMessageMetadata
{
    public string ContentType { get; }
    public string CorrelationId { get; }
    public string SessionId { get; }
    public CancellationToken CancellationToken { get; }
    public IReadOnlyDictionary<string, object> ApplicationProperties { get; }
    public string MessageId { get; }
    public string PartitionKey { get; }
    public string TransactionPartitionKey { get; }
    public string ReplyToSessionId { get; }
    public TimeSpan TimeToLive { get; }
    public string Subject { get; }
    public string To { get; }
    public string ReplyTo { get; }
    public DateTimeOffset ScheduledEnqueueTime { get; }
    public string LockToken { get; }
    public int DeliveryCount { get; }
    public DateTimeOffset LockedUntil { get; }
    public long SequenceNumber { get; }
    public string DeadLetterSource { get; }
    public long EnqueuedSequenceNumber { get; }
    public DateTimeOffset EnqueuedTime { get; }
    public DateTimeOffset ExpiresAt { get; }
    public string DeadLetterReason { get; }
    public string DeadLetterErrorDescription { get; }

    public Task AbandonMessageAsync(IDictionary<string, object>? propertiesToModify = default);
    public Task CompleteMessageAsync();
    public Task DeadLetterMessageAsync(string deadLetterReason, string? deadLetterErrorDescription = default);
    public Task DeadLetterMessageAsync(IDictionary<string, object>? propertiesToModify = default);
    public Task DeferMessageAsync(IDictionary<string, object>? propertiesToModify = default);
}

public class MessageMetadata : IMessageMetadata
{
    private readonly ServiceBusReceivedMessage _message;
    private readonly ProcessSessionMessageEventArgs? _sessionArgs;
    private readonly ProcessMessageEventArgs? _args;

    public MessageMetadata(ServiceBusReceivedMessage message, ProcessMessageEventArgs args,
        CancellationToken token)
    {
        _message = message;
        _args = args;
        CancellationToken = token;
    }

    public MessageMetadata(ServiceBusReceivedMessage message, ProcessSessionMessageEventArgs sessionArgs,
        CancellationToken token)
    {
        _message = message;
        _sessionArgs = sessionArgs;
        CancellationToken = token;
    }

    public async Task AbandonMessageAsync(IDictionary<string, object>? propertiesToModify = default)
    {
        if (_sessionArgs != null)
        {
            await _sessionArgs.AbandonMessageAsync(_message, propertiesToModify, CancellationToken);
        }
        else
        {
            await _args!.AbandonMessageAsync(_message, propertiesToModify, CancellationToken);
        }
    }

    public async Task CompleteMessageAsync()
    {
        if (_sessionArgs != null)
        {
            await _sessionArgs.CompleteMessageAsync(_message, CancellationToken);
        }
        else
        {
            await _args!.CompleteMessageAsync(_message, CancellationToken);
        }
    }

    public async Task DeadLetterMessageAsync(string deadLetterReason, string? deadLetterErrorDescription = default)
    {
        if (_sessionArgs != null)
        {
            await _sessionArgs.DeadLetterMessageAsync(_message, deadLetterReason, deadLetterErrorDescription, CancellationToken);
        }
        else
        {
            await _args!.DeadLetterMessageAsync(_message, deadLetterReason, deadLetterErrorDescription, CancellationToken);
        }
    }

    public async Task DeadLetterMessageAsync(IDictionary<string, object>? propertiesToModify = default)
    {
        if (_sessionArgs != null)
        {
            await _sessionArgs.DeadLetterMessageAsync(_message, propertiesToModify, CancellationToken);
        }
        else
        {
            await _args!.DeadLetterMessageAsync(_message, propertiesToModify, CancellationToken);
        }
    }

    public async Task DeferMessageAsync(IDictionary<string, object>? propertiesToModify = default)
    {
        if (_sessionArgs != null)
        {
            await _sessionArgs.DeferMessageAsync(_message, propertiesToModify, CancellationToken);
        }
        else
        {
            await _args!.DeferMessageAsync(_message, propertiesToModify, CancellationToken);
        }
    }

    public string MessageId => _message.MessageId;
    public string PartitionKey => _message.PartitionKey;
    public string TransactionPartitionKey => _message.TransactionPartitionKey;
    public string ReplyToSessionId => _message.ReplyToSessionId;
    public TimeSpan TimeToLive => _message.TimeToLive;
    public string Subject => _message.Subject;
    public string To => _message.To;
    public string ReplyTo => _message.ReplyTo;
    public DateTimeOffset ScheduledEnqueueTime => _message.ScheduledEnqueueTime;
    public string LockToken => _message.LockToken;
    public int DeliveryCount => _message.DeliveryCount;
    public DateTimeOffset LockedUntil => _message.LockedUntil;
    public long SequenceNumber => _message.SequenceNumber;
    public string DeadLetterSource => _message.DeadLetterSource;
    public long EnqueuedSequenceNumber => _message.EnqueuedSequenceNumber;
    public DateTimeOffset EnqueuedTime => _message.EnqueuedTime;
    public DateTimeOffset ExpiresAt => _message.ExpiresAt;
    public string DeadLetterReason => _message.DeadLetterReason;
    public string DeadLetterErrorDescription => _message.DeadLetterErrorDescription;
    public string SessionId => _message.SessionId;
    public string CorrelationId => _message.CorrelationId;
    public string ContentType => _message.ContentType;
    public IReadOnlyDictionary<string, object> ApplicationProperties => _message.ApplicationProperties;
    public CancellationToken CancellationToken { get; }
}
