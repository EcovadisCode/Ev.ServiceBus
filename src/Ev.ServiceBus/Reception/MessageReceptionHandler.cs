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
    private readonly ServiceBusOptions _serviceBusOptions;

    public MessageReceptionHandler(
        IServiceProvider provider,
        IMessagePayloadSerializer messagePayloadSerializer,
        ILogger<LoggingExtensions.MessageProcessing> logger,
        IMessageMetadataAccessor messageMetadataAccessor,
        IEnumerable<IServiceBusEventListener> eventListeners,
        ServiceBusOptions serviceBusOptions)
    {
        _provider = provider;
        _messagePayloadSerializer = messagePayloadSerializer;
        _logger = logger;
        _messageMetadataAccessor = (MessageMetadataAccessor)messageMetadataAccessor;
        _eventListeners = eventListeners;
        _serviceBusOptions = serviceBusOptions;
        _callHandlerInfo = GetType().GetMethod(nameof(CallHandler), BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public async Task HandleMessageAsync(MessageContext context)
    {
        if (_serviceBusOptions.Settings.UseIsolation && string.IsNullOrEmpty(_serviceBusOptions.Settings.IsolationKey))
        {
            throw new Exception("Isolation key must be set when isolation is enabled");
        }

        using (AddLoggingContext(context))
        {
            _messageMetadataAccessor.SetData(context);

            if (_serviceBusOptions.Settings.UseIsolation)
            {
                var expectedIsolationKey = _serviceBusOptions.Settings.IsolationKey;
                var receivedIsolationKey = context.IsolationKey;
                if (receivedIsolationKey != expectedIsolationKey)
                {
                    _logger.LogInformation("[{expectedIsolationKey}] Ignoring message for another isolation key: {receivedIsolationKey}",
                        expectedIsolationKey, receivedIsolationKey);
                    await _messageMetadataAccessor.Metadata!.AbandonMessageAsync();
                    // We want to give time for other instances to try pick it up
                    await Task.Delay(5000);
                    return;
                }
            }

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

                var executionContext = context.ReadExecutionContext();

                throw new FailedToProcessMessageException(
                    clientType: executionContext.ClientType,
                    resourceId: executionContext.ResourceId,
                    messageId: executionContext.MessageId,
                    payloadTypeId: executionContext.PayloadTypeId,
                    sessionId: executionContext.SessionId,
                    handlerName: executionContext.HandlerName,
                    isolationKey: executionContext.IsolationKey,
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
        var executionContext = context.ReadExecutionContext();

        return _logger.ProcessingInProgress(
            clientType: executionContext.ClientType,
            resourceId: executionContext.ResourceId,
            messageId: executionContext.MessageId,
            payloadTypeId: executionContext.PayloadTypeId,
            sessionId: executionContext.SessionId,
            handlerName: executionContext.HandlerName,
            isolationKey: executionContext.IsolationKey
        );
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
