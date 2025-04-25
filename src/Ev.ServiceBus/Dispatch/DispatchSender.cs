using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Extensions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Management;

namespace Ev.ServiceBus.Dispatch;

public class DispatchSender : IDispatchSender
{
    private const int MaxMessagePerSend = 100;
    private readonly IMessagePayloadSerializer _messagePayloadSerializer;
    private readonly ServiceBusRegistry _dispatchRegistry;
    private readonly ServiceBusRegistry _registry;
    private readonly IMessageMetadataAccessor _messageMetadataAccessor;
    private readonly IEnumerable<IDispatchExtender> _dispatchCustomizers;
    private readonly ServiceBusOptions _serviceBusOptions;

    public DispatchSender(
        ServiceBusRegistry registry,
        IMessagePayloadSerializer messagePayloadSerializer,
        ServiceBusRegistry dispatchRegistry,
        IMessageMetadataAccessor messageMetadataAccessor,
        IEnumerable<IDispatchExtender> dispatchCustomizers,
        ServiceBusOptions serviceBusOptions)
    {
        _registry = registry;
        _messagePayloadSerializer = messagePayloadSerializer;
        _dispatchRegistry = dispatchRegistry;
        _messageMetadataAccessor = messageMetadataAccessor;
        _dispatchCustomizers = dispatchCustomizers;
        _serviceBusOptions = serviceBusOptions;
    }

    /// <inheritdoc />
    public async Task SendDispatch(object messagePayload, CancellationToken token = default)
    {
        var dispatch = new Abstractions.Dispatch(messagePayload);

        await SendDispatch(dispatch, token);
    }

    /// <inheritdoc />
    public async Task SendDispatch(Abstractions.Dispatch messagePayload, CancellationToken token = default)
    {
        var dispatches = CreateMessagesToSend([messagePayload]);

        foreach (var messagePerResource in dispatches)
        {
            var message = messagePerResource.Messages.Single();

            await messagePerResource.Sender.SendMessageAsync(message.Message, token);
        }
    }

    /// <inheritdoc />
    public async Task SendDispatches(IEnumerable<object> messagePayloads, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = messagePayloads.Select(o => new Abstractions.Dispatch(o)).ToArray();
        await SendDispatches(dispatches, token);
    }

    /// <inheritdoc />
    public async Task SendDispatches(IEnumerable<Abstractions.Dispatch> messagePayloads, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = CreateMessagesToSend(messagePayloads);
        foreach (var messagesPerResource in dispatches)
        {
            await BatchAndSendMessages(messagesPerResource, token, async (sender, batch) =>
            {
                await sender.SendMessagesAsync(batch, token);
            });
        }
    }

    private async Task BatchAndSendMessages(MessagesPerResource dispatches, CancellationToken token, Func<IMessageSender, ServiceBusMessageBatch, Task> senderAction)
    {
        var batches = new List<ServiceBusMessageBatch>();
        var batch = await dispatches.Sender.CreateMessageBatchAsync(token);
        batches.Add(batch);
        foreach (var messageToSend in dispatches.Messages)
        {
            if (batch.TryAddMessage(messageToSend.Message))
            {
                continue;
            }
            batch = await dispatches.Sender.CreateMessageBatchAsync(token);
            batches.Add(batch);
            if (batch.TryAddMessage(messageToSend.Message))
            {
                continue;
            }

            throw new ArgumentOutOfRangeException("A message is too big to fit in a single batch");
        }

        foreach (var pageMessages in batches)
        {
            await senderAction.Invoke(dispatches.Sender, pageMessages);
            pageMessages.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task ScheduleDispatches(IEnumerable<object> messagePayloads, DateTimeOffset scheduledEnqueueTime, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = messagePayloads.Select(o => new Abstractions.Dispatch(o)).ToArray();
        await ScheduleDispatches(dispatches, scheduledEnqueueTime, token);
    }

    /// <inheritdoc />
    public async Task ScheduleDispatches(IEnumerable<Abstractions.Dispatch> messagePayloads, DateTimeOffset scheduledEnqueueTime, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = CreateMessagesToSend(messagePayloads);
        foreach (var messagesPerResource in dispatches)
        {
            await PaginateAndSendMessages(messagesPerResource, async (sender, page) =>
            {
                await sender.ScheduleMessagesAsync(page, scheduledEnqueueTime, token);
            });
        }
    }

    private async Task PaginateAndSendMessages(MessagesPerResource dispatches, Func<IMessageSender, IEnumerable<ServiceBusMessage>, Task> senderAction)
    {
        var paginatedMessages = dispatches.Messages.Select(o => o.Message)
            .Select((x, i) => new
            {
                Item = x,
                Index = i
            })
            .GroupBy(x => x.Index / MaxMessagePerSend, x => x.Item);

        foreach (var pageMessages in paginatedMessages)
        {
            await senderAction.Invoke(dispatches.Sender, pageMessages.Select(m => m).ToArray());
        }
    }

    private class MessagesPerResource
    {
        public MessageToSend[] Messages { get; set; }
        public ClientType ClientType { get; set; }
        public string ResourceId { get; set; }
        public IMessageSender Sender { get; set; }
    }

    private class MessageToSend
    {
        public MessageToSend(ServiceBusMessage message, MessageDispatchRegistration registration)
        {
            Message = message;
            Registration = registration;
        }

        public ServiceBusMessage Message { get; }
        public MessageDispatchRegistration Registration { get; }
    }

    private MessagesPerResource[] CreateMessagesToSend(IEnumerable<Abstractions.Dispatch> messagePayloads)
    {
        var dispatches =
            (
                from dispatch in messagePayloads
                // the same dispatch can be published to several senders
                let registrations = _dispatchRegistry.GetDispatchRegistrations(dispatch.Payload.GetType())
                from eventPublicationRegistration in registrations
                let message = CreateMessage(eventPublicationRegistration, dispatch)
                select new MessageToSend(message, eventPublicationRegistration)
            )
            .ToArray();

        foreach (var item in dispatches)
        {
            item.Message.SetIsolationKey(_serviceBusOptions.Settings.IsolationKey);
        }

        var messagesPerResource = (
            from dispatch in dispatches
            group dispatch by new { dispatch.Registration.Options.ClientType, dispatch.Registration.Options.ResourceId } into gr
            let sender = _registry.GetMessageSender(gr.Key.ClientType, gr.Key.ResourceId)
            select new MessagesPerResource()
            {
                Messages = gr.ToArray(),
                ClientType = gr.Key.ClientType,
                ResourceId = gr.Key.ResourceId,
                Sender = sender
            }).ToArray();

        return messagesPerResource;
    }

    private ServiceBusMessage CreateMessage(
        MessageDispatchRegistration registration,
        Abstractions.Dispatch dispatch)
    {
        var originalCorrelationId = _messageMetadataAccessor.Metadata?.CorrelationId ?? Guid.NewGuid().ToString();
        var originalIsolationKey = _messageMetadataAccessor.Metadata?.ApplicationProperties.GetIsolationKey();
        var result = _messagePayloadSerializer.SerializeBody(dispatch.Payload);
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, registration.PayloadTypeId);

        dispatch.ApplicationProperties.Remove(UserProperties.PayloadTypeIdProperty);
        foreach (var dispatchApplicationProperty in dispatch.ApplicationProperties)
        {
            message.ApplicationProperties[dispatchApplicationProperty.Key] = dispatchApplicationProperty.Value;
        }

        message.SessionId = dispatch.SessionId;
        message.CorrelationId = dispatch.CorrelationId ?? originalCorrelationId;
        message.SetIsolationKey(dispatch.ApplicationProperties.GetIsolationKey() ?? originalIsolationKey);
        if (dispatch.DiagnosticId != null)
            message.SetDiagnosticIdIfIsNot(dispatch.DiagnosticId);
        if (!string.IsNullOrWhiteSpace(dispatch.MessageId))
        {
            message.MessageId = dispatch.MessageId;
        }

        foreach (var customizer in registration.OutgoingMessageCustomizers)
        {
            customizer?.Invoke(message, dispatch.Payload);
        }

        foreach (var dispatchCustomizer in _dispatchCustomizers)
        {
            dispatchCustomizer.ExtendDispatch(message, dispatch.Payload);
        }
        return message;
    }
}