using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Extensions;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.Reception.Extensions;

public static class MessageContextExtensions
{
    public static async Task CompleteAndResendMessageAsync(
        this MessageContext context,
        IMessagePayloadSerializer messagePayloadSerializer,
        MessageMetadataAccessor messageMetadataAccessor,
        IServiceProvider provider)
    {
        var messageBody = context.Message.Body.ToArray();
        var sessionId = context.SessionArgs?.SessionId;
        var correlationId = context.Message.CorrelationId;
        var messageId = context.Message.MessageId;

        Dictionary<string, object> applicationProps = new();
        foreach (var prop in context.Message.ApplicationProperties)
        {
            if (!prop.Key.Contains("DeliveryCount", StringComparison.OrdinalIgnoreCase))
            {
                applicationProps[prop.Key] = prop.Value;
            }
        }

        var originalPayload = messagePayloadSerializer.DeSerializeBody(
            messageBody,
            context.ReceptionRegistration?.PayloadType ?? typeof(object)
        );

        await messageMetadataAccessor.Metadata!.CompleteMessageAsync();

        var newDispatch = new Abstractions.Dispatch(originalPayload)
        {
            SessionId = sessionId,
            CorrelationId = correlationId,
            MessageId = messageId,
            DiagnosticId = context.Message.GetDiagnosticId() ?? Activity.Current?.Id
        };

        foreach (var prop in applicationProps)
        {
            newDispatch.ApplicationProperties[prop.Key] = prop.Value;
        }

        var sender = provider.GetRequiredService<IDispatchSender>();
        await sender.SendDispatch(newDispatch);
    }
}