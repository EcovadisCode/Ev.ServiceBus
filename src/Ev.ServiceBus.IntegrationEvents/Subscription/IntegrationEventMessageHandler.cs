using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class IntegrationEventMessageHandler : IMessageHandler
    {
        private readonly MethodInfo _callHandlerInfo;
        private readonly IServiceProvider _provider;
        private readonly ServiceBusEventSubscriptionRegistry _registry;
        private readonly ILogger<IntegrationEventMessageHandler> _logger;
        private readonly IMessageBodyParser _messageBodyParser;

        public IntegrationEventMessageHandler(
            IServiceProvider provider,
            ServiceBusEventSubscriptionRegistry registry,
            ILogger<IntegrationEventMessageHandler> logger,
            IMessageBodyParser messageBodyParser)
        {
            _provider = provider;
            _registry = registry;
            _logger = logger;
            _messageBodyParser = messageBodyParser;
            _callHandlerInfo = GetType().GetMethod(nameof(CallHandler), BindingFlags.NonPublic | BindingFlags.Instance)!;
        }

        public async Task HandleMessageAsync(MessageContext context)
        {
            var eventTypeId = context.Message.GetEventTypeId();
            if (eventTypeId == null)
            {
                throw new MessageIsMissingEventTypeIdException(context);
            }

            var eventSubscriptions = _registry.GetRegistrations(
                eventTypeId,
                context.Receiver.Name,
                context.Receiver.ClientType);

            var hasFailed = false;
            foreach (var eventSubscription in eventSubscriptions)
            {
                if (context.Token.IsCancellationRequested)
                {
                    _logger.LogInformation($"[Ev.ServiceBus.IntegrationEvents] Stopping the execution because cancellation was requested.");
                    return;
                }
                var @event = _messageBodyParser.DeSerializeBody(context.Message.Body, eventSubscription.EventType);
                var methodInfo = _callHandlerInfo.MakeGenericMethod(eventSubscription.EventType);
                try
                {
                    _logger.LogDebug($"[Ev.ServiceBus.IntegrationEvents] Executing {eventSubscription.EventTypeId}:{eventSubscription.HandlerType.FullName} handler.");
                    await ((Task) methodInfo.Invoke(this, new[] { eventSubscription, @event, context.Token })!).ConfigureAwait(false);
                    _logger.LogDebug($"[Ev.ServiceBus.IntegrationEvents] Execution of  {eventSubscription.EventTypeId}:{eventSubscription.HandlerType.FullName} handler successful.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Ev.ServiceBus.IntegrationEvents] Handler {eventSubscription.EventTypeId}:{eventSubscription.HandlerType.FullName} failed.\n"
                                         + $"Receiver : {context.Receiver.ClientType} | {context.Receiver.Name}\n");
                    hasFailed = true;
                }
            }

            if (hasFailed)
            {
                _logger.LogWarning($"[Ev.ServiceBus.IntegrationEvents] Abandoning the message since at least one handler has failed.");
                await context.Receiver.AbandonAsync(context.Message.SystemProperties.LockToken).ConfigureAwait(false);
            }
            else
            {
                await context.Receiver.CompleteAsync(context.Message.SystemProperties.LockToken).ConfigureAwait(false);
            }
        }

        private async Task CallHandler<TIntegrationEvent>(EventSubscriptionRegistration eventSubscriptionRegistration, TIntegrationEvent @event, CancellationToken token)
        {
            var handler = (IIntegrationEventHandler<TIntegrationEvent>) _provider.GetRequiredService(
                    eventSubscriptionRegistration.HandlerType);

            await handler.Handle(@event, token).ConfigureAwait(false);
        }
    }
}
