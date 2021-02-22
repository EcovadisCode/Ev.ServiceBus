using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Reception
{
    public class MessageReceptionHandler : IMessageHandler
    {
        private readonly MethodInfo _callHandlerInfo;
        private readonly ILogger<MessageReceptionHandler> _logger;
        private readonly IMessagePayloadParser _messagePayloadParser;
        private readonly IServiceProvider _provider;
        private readonly ReceptionRegistry _registry;

        public MessageReceptionHandler(
            IServiceProvider provider,
            ReceptionRegistry registry,
            ILogger<MessageReceptionHandler> logger,
            IMessagePayloadParser messagePayloadParser)
        {
            _provider = provider;
            _registry = registry;
            _logger = logger;
            _messagePayloadParser = messagePayloadParser;
            _callHandlerInfo =
                GetType().GetMethod(nameof(CallHandler), BindingFlags.NonPublic | BindingFlags.Instance)!;
        }

        public async Task HandleMessageAsync(MessageContext context)
        {
            var payloadTypeId = context.Message.GetPayloadTypeId() ?? context.Message.GetEventTypeId();
            if (payloadTypeId == null)
            {
                throw new MessageIsMissingPayloadTypeIdException(context);
            }

            var receptionRegistration = _registry.GetRegistration(
                payloadTypeId,
                context.Receiver.Name,
                context.Receiver.ClientType);

            if (receptionRegistration == null)
            {
                return;
            }

            if (context.Token.IsCancellationRequested)
            {
                _logger.LogInformation("[Ev.ServiceBus] Stopping the execution because cancellation was requested");
                return;
            }

            var @event = _messagePayloadParser.DeSerializeBody(context.Message.Body, receptionRegistration.PayloadType);
            var methodInfo = _callHandlerInfo.MakeGenericMethod(receptionRegistration.PayloadType);
            try
            {
                _logger.LogDebug(
                    $"[Ev.ServiceBus] Executing {receptionRegistration.PayloadTypeId}:{receptionRegistration.HandlerType.FullName} handler");
                await ((Task) methodInfo.Invoke(this, new[] { receptionRegistration, @event, context.Token })!).ConfigureAwait(false);
                _logger.LogDebug(
                    $"[Ev.ServiceBus] Execution of  {receptionRegistration.PayloadTypeId}:{receptionRegistration.HandlerType.FullName} handler successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"[Ev.ServiceBus] Handler {receptionRegistration.PayloadTypeId}:{receptionRegistration.HandlerType.FullName} failed.\n"
                    + $"Receiver : {context.Receiver.ClientType} | {context.Receiver.Name}\n");
            }
        }

        private async Task CallHandler<TMessagePayload>(
            MessageReceptionRegistration messageReceptionRegistration,
            TMessagePayload @event,
            CancellationToken token)
        {
            var handler = (IMessageReceptionHandler<TMessagePayload>) _provider.GetRequiredService(
                messageReceptionRegistration.HandlerType);

            await handler.Handle(@event, token).ConfigureAwait(false);
        }
    }
}
