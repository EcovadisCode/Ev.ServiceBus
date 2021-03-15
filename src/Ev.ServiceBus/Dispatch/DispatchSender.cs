using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Dispatch
{
    public class DispatchSender : IDispatchSender
    {
        private const int MaxMessagePerSend = 100;
        private readonly IMessagePayloadSerializer _messagePayloadSerializer;
        private readonly DispatchRegistry _dispatchRegistry;
        private readonly IServiceBusRegistry _registry;

        public DispatchSender(
            IServiceBusRegistry registry,
            IMessagePayloadSerializer messagePayloadSerializer,
            DispatchRegistry dispatchRegistry)
        {
            _registry = registry;
            _messagePayloadSerializer = messagePayloadSerializer;
            _dispatchRegistry = dispatchRegistry;
        }

        public async Task SendDispatches(IEnumerable<object> messagePayloads)
        {
            if (messagePayloads == null)
            {
                throw new ArgumentNullException(nameof(messagePayloads));
            }

            var dispatches =
                (
                    from dto in messagePayloads
                    // the same dto can be published to several senders
                    let registrations = _dispatchRegistry.GetRegistrations(dto.GetType())
                    from eventPublicationRegistration in registrations
                    let message = CreateMessage(eventPublicationRegistration, dto)
                    select new Dispatch(message, eventPublicationRegistration))
                .ToArray();

            foreach (var groupedDispatch in dispatches
                .GroupBy(o => new { o.Registration.Options.ClientType,
                    o.Registration.Options.ResourceId }))
            {
                var sender = groupedDispatch.Key.ClientType == ClientType.Queue
                    ? _registry.GetQueueSender(groupedDispatch.Key.ResourceId)
                    : _registry.GetTopicSender(groupedDispatch.Key.ResourceId);

                var paginatedMessages = groupedDispatch.Select(o => o.Message)
                    .Select((x, i) => new { Item = x, Index = i })
                    .GroupBy(x => x.Index / MaxMessagePerSend, x => x.Item);

                foreach (var pageMessages in paginatedMessages)
                {
                    await sender.SendAsync(pageMessages.Select(m => m).ToArray()).ConfigureAwait(false);
                }
            }
        }

        private Message CreateMessage(MessageDispatchRegistration registration, object dto)
        {
            var result = _messagePayloadSerializer.SerializeBody(dto);
            var message = MessageHelper.CreateMessage(result.ContentType, result.Body, registration.PayloadTypeId);
            foreach (var customizer in registration.OutgoingMessageCustomizers)
            {
                customizer?.Invoke(message, dto);
            }

            return message;
        }

        private class Dispatch
        {
            public Dispatch(Message message, MessageDispatchRegistration registration)
            {
                this.Message = message;
                this.Registration = registration;
            }

            public Message Message { get; }
            public MessageDispatchRegistration Registration { get; }
        }
    }
}
