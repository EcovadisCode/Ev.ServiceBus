using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions;

/// <summary>
///     Exposes all the methods available for sending a message through a queue.
/// </summary>
public interface IMessageSender : IClient
{
    /// <summary>
    ///   Sends a message to the associated entity of Service Bus.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    ///
    /// <returns>A task to be resolved on when the operation has completed.</returns>
    /// <exception cref="ServiceBusException">
    ///   The message exceeds the maximum size allowed, as determined by the Service Bus service.
    ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded"/> in this case.
    ///   For more information on service limits, see
    ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas"/>.
    /// </exception>
    Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    ///   Sends a set of messages to the associated Service Bus entity using a batched approach.
    ///   If the size of the messages exceed the maximum size of a single batch,
    ///   an exception will be triggered and the send will fail. In order to ensure that the messages
    ///   being sent will fit in a batch, use <see cref="SendMessagesAsync(ServiceBusMessageBatch, CancellationToken)"/> instead.
    /// </summary>
    ///
    /// <param name="messages">The set of messages to send.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    ///
    /// <returns>A task to be resolved on when the operation has completed.</returns>
    /// <exception cref="ServiceBusException">
    ///   The set of messages exceeds the maximum size allowed in a single batch, as determined by the Service Bus service.
    ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded"/> in this case.
    ///   For more information on service limits, see
    ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas"/>.
    /// </exception>
    Task SendMessagesAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    ///   Creates a size-constraint batch to which <see cref="ServiceBusMessage" /> may be added using
    ///   a <see cref="ServiceBusMessageBatch.TryAddMessage"/>. If a message would exceed the maximum
    ///   allowable size of the batch, the batch will not allow adding the message and signal that
    ///   scenario using it return value.
    ///
    ///   Because messages that would violate the size constraint cannot be added, publishing a batch
    ///   will not trigger an exception when attempting to send the messages to the Queue/Topic.
    /// </summary>
    ///
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    ///
    /// <returns>An <see cref="ServiceBusMessageBatch" /> with the default batch options.</returns>
    ///
    /// <seealso cref="CreateMessageBatchAsync(CreateMessageBatchOptions, CancellationToken)" />
    ///
    ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///   Creates a size-constraint batch to which <see cref="ServiceBusMessage" /> may be added using a try-based pattern.  If a message would
    ///   exceed the maximum allowable size of the batch, the batch will not allow adding the message and signal that scenario using its
    ///   return value.
    ///
    ///   Because messages that would violate the size constraint cannot be added, publishing a batch will not trigger an exception when
    ///   attempting to send the messages to the Queue/Topic.
    /// </summary>
    ///
    /// <param name="options">The set of options to consider when creating this batch.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    ///
    /// <returns>An <see cref="ServiceBusMessageBatch" /> with the requested <paramref name="options"/>.</returns>
    ///
    /// <seealso cref="CreateMessageBatchAsync(CreateMessageBatchOptions, CancellationToken)" />
    ///
    ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CreateMessageBatchOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///   Sends a <see cref="ServiceBusMessageBatch"/>
    ///   containing a set of <see cref="ServiceBusMessage"/> to
    ///   the associated Service Bus entity.
    /// </summary>
    ///
    /// <param name="messageBatch">The batch of messages to send. A batch may be created using <see cref="CreateMessageBatchAsync(CancellationToken)" />.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    /// <returns>A task to be resolved on when the operation has completed.</returns>
    ///
    Task SendMessagesAsync(ServiceBusMessageBatch messageBatch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a message to appear on Service Bus at a later time.
    /// </summary>
    ///
    /// <param name="message">The <see cref="ServiceBusMessage"/> to schedule.</param>
    /// <param name="scheduledEnqueueTime">The UTC time at which the message should be available for processing</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    ///
    /// <remarks>Although the message will not be available to be received until the scheduledEnqueueTime, it can still be peeked before that time.
    /// Messages can also be scheduled by setting <see cref="ServiceBusMessage.ScheduledEnqueueTime"/> and
    /// using <see cref="SendMessageAsync(ServiceBusMessage, CancellationToken)"/>,
    /// <see cref="SendMessagesAsync(IEnumerable{ServiceBusMessage}, CancellationToken)"/>, or
    /// <see cref="SendMessagesAsync(ServiceBusMessageBatch, CancellationToken)"/>.</remarks>
    ///
    /// <returns>The sequence number of the message that was scheduled.</returns>
    Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a set of messages to appear on Service Bus at a later time.
    /// </summary>
    ///
    /// <param name="messages">The set of messages to schedule.</param>
    /// <param name="scheduledEnqueueTime">The UTC time at which the message should be available for processing</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    ///
    /// <remarks>Although the message will not be available to be received until the scheduledEnqueueTime, it can still be peeked before that time.
    /// Messages can also be scheduled by setting <see cref="ServiceBusMessage.ScheduledEnqueueTime"/> and
    /// using <see cref="SendMessageAsync(ServiceBusMessage, CancellationToken)"/>,
    /// <see cref="SendMessagesAsync(IEnumerable{ServiceBusMessage}, CancellationToken)"/>, or
    /// <see cref="SendMessagesAsync(ServiceBusMessageBatch, CancellationToken)"/>.</remarks>
    ///
    /// <returns>The sequence number of the message that was scheduled.</returns>
    Task<IReadOnlyList<long>> ScheduleMessagesAsync(IEnumerable<ServiceBusMessage> messages,
        DateTimeOffset scheduledEnqueueTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a message that was scheduled.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="ServiceBusReceivedMessage.SequenceNumber"/> of the message to be cancelled.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a set of messages that were scheduled.
    /// </summary>
    /// <param name="sequenceNumbers">The set of <see cref="ServiceBusReceivedMessage.SequenceNumber"/> of the messages to be cancelled.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
    Task CancelScheduledMessagesAsync(IEnumerable<long> sequenceNumbers,
        CancellationToken cancellationToken = default);
}