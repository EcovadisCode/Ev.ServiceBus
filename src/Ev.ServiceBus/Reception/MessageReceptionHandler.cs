using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Reception;

public class MessageReceptionHandler
{
    private readonly MethodInfo _callHandlerInfo;
    private readonly IMessagePayloadSerializer _messagePayloadSerializer;
    private readonly ILogger<MessageReceptionHandler> _logger;
    private readonly MessageMetadataAccessor _messageMetadataAccessor;
    private readonly IEnumerable<IServiceBusEventListener> _eventListeners;
    private readonly IServiceProvider _provider;

    public MessageReceptionHandler(
        IServiceProvider provider,
        IMessagePayloadSerializer messagePayloadSerializer,
        ILogger<MessageReceptionHandler> logger,
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
        var scopeValues = new Dictionary<string, string>
        {
            ["EVSB_Client"] = context.ClientType.ToString(),
            ["EVSB_ResourceId"] = context.ResourceId,
            ["EVSB_MessageId"] = context.Message.MessageId,
            ["EVSB_SessionId"] = context.SessionArgs?.SessionId ?? "none",
            ["EVSB_PayloadTypeId"] = context.PayloadTypeId ?? "none",
            ["EVSB_ReceptionHandler"] = context.ReceptionRegistration?.HandlerType.FullName ?? "none"
        };
        using (_logger.BeginScope(scopeValues))
        {
            _messageMetadataAccessor.SetData(context);

            var executionStartedArgs = new ExecutionStartedArgs(context);
            foreach (var listener in _eventListeners)
            {
                await listener.OnExecutionStart(executionStartedArgs);
            }
            _logger.LogInformation("[Ev.ServiceBus] New message received from {EVSB_Client} '{EVSB_ResourceId}' : {EVSB_MessageLabel}",
                context.ClientType,
                context.ResourceId,
                context.Message.Subject);

            var sw = new Stopwatch();
            sw.Start();
            try
            {
                if (context.PayloadTypeId == null)
                {
                    throw new MessageIsMissingPayloadTypeIdException(context);
                }

                if (context.ReceptionRegistration == null)
                {
                    return;
                }

                if (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("[Ev.ServiceBus] Stopping the execution because cancellation was requested");
                    return;
                }

                _logger.LogDebug(
                    "[Ev.ServiceBus] Executing {EVSB_PayloadTypeId}:{EVSB_ReceptionHandler} handler",
                    context.ReceptionRegistration.PayloadTypeId,
                    context.ReceptionRegistration.HandlerType.FullName);

                var @event = _messagePayloadSerializer.DeSerializeBody(context.Message.Body.ToArray(), context.ReceptionRegistration!.PayloadType);
                var methodInfo = _callHandlerInfo.MakeGenericMethod(context.ReceptionRegistration.PayloadType);
                await ((Task) methodInfo.Invoke(this, new[] { context.ReceptionRegistration, @event, context.CancellationToken })!);

                _logger.LogInformation(
                    "[Ev.ServiceBus] Execution of {EVSB_PayloadTypeId}:{EVSB_ReceptionHandler} reception handler successful in {EVSB_Duration} milliseconds",
                    context.ReceptionRegistration.PayloadTypeId,
                    context.ReceptionRegistration.HandlerType.FullName,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex) when (LogError(ex))
            {
                var executionFailedArgs = new ExecutionFailedArgs(context, ex);
                foreach (var listener in _eventListeners)
                {
                    await listener.OnExecutionFailed(executionFailedArgs);
                }
                throw;
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
            _logger.LogInformation("[Ev.ServiceBus] Message finished execution in {EVSB_Duration} milliseconds", sw.ElapsedMilliseconds);
        }
    }

    private async Task CallHandler<TMessagePayload>(
        MessageReceptionRegistration messageReceptionRegistration,
        TMessagePayload @event,
        CancellationToken token)
    {
        var handler = (IMessageReceptionHandler<TMessagePayload>) _provider.GetRequiredService(messageReceptionRegistration.HandlerType);

        await handler.Handle(@event, token);
    }

    /// <summary>
    ///     workaround to attach the log scope to the logged exception
    ///     https://andrewlock.net/how-to-include-scopes-when-logging-exceptions-in-asp-net-core/
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    private bool LogError(Exception ex)
    {
        _logger.LogError(ex, ex.Message);
        return true;
    }
}
