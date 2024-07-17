using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Exceptions;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Reception;

public class MessageReceptionHandler
{
    private readonly MethodInfo _callHandlerInfo;
    private readonly IMessagePayloadSerializer _messagePayloadSerializer;
    private readonly ILogger<LoggingExtensions.MessageProcessing> _logger;
    private readonly MessageMetadataAccessor _messageMetadataAccessor;
    private readonly IEnumerable<IServiceBusEventListener> _eventListeners;
    private readonly IServiceProvider _provider;

    public MessageReceptionHandler(
        IServiceProvider provider,
        IMessagePayloadSerializer messagePayloadSerializer,
        ILogger<LoggingExtensions.MessageProcessing> logger,
        IMessageMetadataAccessor messageMetadataAccessor,
        IEnumerable<IServiceBusEventListener> eventListeners)
    {
        _provider = provider;
        _messagePayloadSerializer = messagePayloadSerializer;
        _logger = logger;
        _messageMetadataAccessor = (MessageMetadataAccessor)messageMetadataAccessor;
        _eventListeners = eventListeners;
        _callHandlerInfo = GetType().GetMethod(nameof(CallHandler), BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public async Task HandleMessageAsync(MessageContext context)
    {
        using (AddLoggingContext(context))
        {
            _messageMetadataAccessor.SetData(context);

            var executionStartedArgs = new ExecutionStartedArgs(context);
            foreach (var listener in _eventListeners)
            {
                await listener.OnExecutionStart(executionStartedArgs);
            }

            var sw = new Stopwatch();
            sw.Start();
            try
            {
                if (context.PayloadTypeId == null)
                    throw new MessageIsMissingPayloadTypeIdException(context);

                if (context.ReceptionRegistration == null)
                    return;

                if (context.CancellationToken.IsCancellationRequested)
                    return;

                var @event = _messagePayloadSerializer.DeSerializeBody(context.Message.Body.ToArray(), context.ReceptionRegistration!.PayloadType);
                var methodInfo = _callHandlerInfo.MakeGenericMethod(context.ReceptionRegistration.PayloadType);
                await ((Task) methodInfo.Invoke(this, new[] { context.ReceptionRegistration, @event, context.CancellationToken })!);
            }
            catch (Exception ex)
            {
                var executionFailedArgs = new ExecutionFailedArgs(context, ex);
                foreach (var listener in _eventListeners)
                {
                    await listener.OnExecutionFailed(executionFailedArgs);
                }

                throw new FailedToProcessMessageException(
                    clientType: GetClientType(context),
                    resourceId: GetContextResourceId(context),
                    messageId: GetMessageId(context),
                    payloadTypeId: GetPayloadTypeId(context),
                    sessionId: GetSessionId(context),
                    handlerName: GetHandlerTypeFullName(context),
                    ex);
            }
            finally
            {
                sw.Stop();
            }

            var executionSucceededArgs = new ExecutionSucceededArgs(context, sw.ElapsedMilliseconds);
            foreach (var listener in _eventListeners)
            {
                await listener.OnExecutionSuccess(executionSucceededArgs);
            }
            _logger.MessageExecutionCompleted(sw.ElapsedMilliseconds);
        }
    }

    private IDisposable? AddLoggingContext(MessageContext context)
    {
        var clientType = GetClientType(context);
        var contextResourceId = GetContextResourceId(context);
        var messageMessageId = GetMessageId(context);
        var contextPayloadTypeId = GetPayloadTypeId(context);
        var sessionArgsSessionId = GetSessionId(context);
        var handlerTypeFullName = GetHandlerTypeFullName(context);

        return _logger.ProcessingInProgress(
            clientType: clientType,
            resourceId: contextResourceId,
            messageId: messageMessageId,
            payloadTypeId: contextPayloadTypeId,
            sessionId: sessionArgsSessionId,
            handlerName: handlerTypeFullName
        );
    }

    private static string GetHandlerTypeFullName(MessageContext context)
    {
        return context.ReceptionRegistration?.HandlerType.FullName ?? "none";
    }

    private static string GetSessionId(MessageContext context)
    {
        return context.SessionArgs?.SessionId ?? "none";
    }

    private static string GetPayloadTypeId(MessageContext context)
    {
        return context.PayloadTypeId ?? "none";
    }

    private static string GetMessageId(MessageContext context)
    {
        return context.Message.MessageId;
    }

    private static string GetContextResourceId(MessageContext context)
    {
        return context.ResourceId;
    }

    private static string GetClientType(MessageContext context)
    {
        return context.ClientType.ToString();
    }

    private async Task CallHandler<TMessagePayload>(
        MessageReceptionRegistration messageReceptionRegistration,
        TMessagePayload @event,
        CancellationToken token)
    {
        var handler = (IMessageReceptionHandler<TMessagePayload>) _provider.GetRequiredService(messageReceptionRegistration.HandlerType);

        await handler.Handle(@event, token);
    }
}
