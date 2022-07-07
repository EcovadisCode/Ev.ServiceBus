using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Reception;

public class MessageReceptionHandler : IMessageHandler
{
    private readonly MethodInfo _callHandlerInfo;
    private readonly ILogger<MessageReceptionHandler> _logger;
    private readonly IMessagePayloadSerializer _messagePayloadSerializer;
    private readonly IServiceProvider _provider;
    private readonly ServiceBusRegistry _registry;

    public MessageReceptionHandler(IServiceProvider provider,
        ServiceBusRegistry registry,
        ILogger<MessageReceptionHandler> logger,
        IMessagePayloadSerializer messagePayloadSerializer)
    {
        _provider = provider;
        _registry = registry;
        _logger = logger;
        _messagePayloadSerializer = messagePayloadSerializer;
        _callHandlerInfo =
            GetType().GetMethod(nameof(CallHandler), BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public async Task HandleMessageAsync(MessageContext context)
    {
        var payloadTypeId = context.Message.GetPayloadTypeId();
        if (payloadTypeId == null)
        {
            throw new MessageIsMissingPayloadTypeIdException(context);
        }

        var receptionRegistration = _registry.GetReceptionRegistration(payloadTypeId,
            context.ResourceId,
            context.ClientType);

        if (receptionRegistration == null)
        {
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("[Ev.ServiceBus] Stopping the execution because cancellation was requested");
            return;
        }

        var @event = _messagePayloadSerializer.DeSerializeBody(context.Message.Body.ToArray(), receptionRegistration.PayloadType);
        var methodInfo = _callHandlerInfo.MakeGenericMethod(receptionRegistration.PayloadType);
        var scopeValues = new Dictionary<string, string>
        {
            ["EVSB_PayloadTypeId"] = receptionRegistration.PayloadTypeId,
            ["EVSB_ReceptionHandler"] = receptionRegistration.HandlerType.FullName!
        };
        var sw = new Stopwatch();
        using (_logger.BeginScope(scopeValues))
        {
            _logger.LogDebug("[Ev.ServiceBus] Executing {EVSB_PayloadTypeId}:{EVSB_ReceptionHandler} handler",
                receptionRegistration.PayloadTypeId, receptionRegistration.HandlerType.FullName);
            sw.Start();
            await ((Task) methodInfo.Invoke(this, new[] { receptionRegistration, @event, context.CancellationToken })!).ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation(
                "[Ev.ServiceBus] Execution of {EVSB_PayloadTypeId}:{EVSB_ReceptionHandler} reception handler successful in {EVSB_Duration} milliseconds",
                receptionRegistration.PayloadTypeId, receptionRegistration.HandlerType.FullName,
                sw.ElapsedMilliseconds);
        }
    }

    private async Task CallHandler<TMessagePayload>(MessageReceptionRegistration messageReceptionRegistration,
        TMessagePayload @event,
        CancellationToken token)
    {
        var handler =
            (IMessageReceptionHandler<TMessagePayload>) _provider.GetRequiredService(messageReceptionRegistration
                .HandlerType);

        await handler.Handle(@event, token).ConfigureAwait(false);
    }
}