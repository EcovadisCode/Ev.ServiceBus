using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Extensions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus.Dispatch;

public record MessagesPerResource(ServiceBusMessage[] Messages, string ResourceId);

public class ServiceBusMessageFactory
{
    private readonly ServiceBusRegistry _registry;
    private readonly IMessagePayloadSerializer _messagePayloadSerializer;
    private readonly IMessageMetadataAccessor _messageMetadataAccessor;
    private readonly IEnumerable<IDispatchExtender> _dispatchCustomizers;
    private readonly ServiceBusOptions _serviceBusOptions;

    public ServiceBusMessageFactory(
        ServiceBusRegistry serviceBusRegistry,
        IMessagePayloadSerializer messagePayloadSerializer,
        IMessageMetadataAccessor messageMetadataAccessor,
        IEnumerable<IDispatchExtender> dispatchCustomizers,
        IOptions<ServiceBusOptions> serviceBusOptions
    )
    {
        _registry = serviceBusRegistry;
        _messagePayloadSerializer = messagePayloadSerializer;
        _messageMetadataAccessor = messageMetadataAccessor;
        _dispatchCustomizers = dispatchCustomizers;
        _serviceBusOptions = serviceBusOptions.Value;
    }

    private record MessageToSend(ServiceBusMessage Message, string ResourceId);

    public MessagesPerResource[] CreateMessagesToSend(IEnumerable<Abstractions.Dispatch> messagePayloads)
    {
        var dispatches =
            (
                from dispatch in messagePayloads
                // the same dispatch can be published to several senders
                let registrations = _registry.GetDispatchRegistrations(dispatch.Payload.GetType())
                from eventPublicationRegistration in registrations
                let message = CreateMessage(eventPublicationRegistration, dispatch)
                select new MessageToSend(message, eventPublicationRegistration.Options.ResourceId)
            )
            .ToArray();

        var messagesPerResource = (
            from dispatch in dispatches
            group dispatch by dispatch.ResourceId into gr
            select new MessagesPerResource(gr.Select(o => o.Message).ToArray(), gr.Key))
            .ToArray();

        return messagesPerResource;
    }

    private ServiceBusMessage CreateMessage(
        MessageDispatchRegistration registration,
        Abstractions.Dispatch dispatch)
    {
        var result = _messagePayloadSerializer.SerializeBody(dispatch.Payload);
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, registration.PayloadTypeId);

        dispatch.ApplicationProperties.Remove(UserProperties.PayloadTypeIdProperty);
        foreach (var dispatchApplicationProperty in dispatch.ApplicationProperties)
        {
            message.ApplicationProperties[dispatchApplicationProperty.Key] = dispatchApplicationProperty.Value;
        }

        message.SessionId = dispatch.SessionId;

        var originalCorrelationId = _messageMetadataAccessor.Metadata?.CorrelationId ?? Guid.NewGuid().ToString();
        message.CorrelationId = dispatch.CorrelationId ?? originalCorrelationId;

        var originalIsolationKey = _messageMetadataAccessor.Metadata?.ApplicationProperties.GetIsolationKey();
        message.SetIsolationKey(originalIsolationKey ?? _serviceBusOptions.Settings.IsolationSettings.IsolationKey);

        var originalIsolationApps = _messageMetadataAccessor.Metadata?.ApplicationProperties.GetIsolationApps() ?? [];
        message.SetIsolationApps(originalIsolationApps);

        if (dispatch.DiagnosticId != null)
        {
            message.SetDiagnosticIdIfIsNot(dispatch.DiagnosticId);
        }
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
