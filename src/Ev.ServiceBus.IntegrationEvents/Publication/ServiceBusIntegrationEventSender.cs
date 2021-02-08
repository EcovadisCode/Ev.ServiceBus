using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class ServiceBusIntegrationEventSender : IIntegrationEventSender
    {
        private const int MaxMessagePerSend = 100;
        private readonly IMessageBodyParser _messageBodyParser;
        private readonly PublicationRegistry _publicationRegistry;
        private readonly IServiceBusRegistry _registry;

        public ServiceBusIntegrationEventSender(
            IServiceBusRegistry registry,
            IMessageBodyParser messageBodyParser,
            PublicationRegistry publicationRegistry)
        {
            _registry = registry;
            _messageBodyParser = messageBodyParser;
            _publicationRegistry = publicationRegistry;
        }

        public async Task SendEvents(IEnumerable<object> messageDtos)
        {
            if (messageDtos == null)
            {
                throw new ArgumentNullException(nameof(messageDtos));
            }

            var dispatches =
                (
                    from dto in messageDtos
                    // the same dto can be published to several senders
                    let registrations = _publicationRegistry.GetRegistrations(dto.GetType())
                    from eventPublicationRegistration in registrations
                    let message = CreateMessage(eventPublicationRegistration, dto)
                    select new Dispatch(message, eventPublicationRegistration))
                .ToArray();

            foreach (var groupedDispatch in dispatches
                .GroupBy(o => new { o.Registration.ClientType, o.Registration.SenderName }))
            {
                var sender = groupedDispatch.Key.ClientType == ClientType.Queue
                    ? _registry.GetQueueSender(groupedDispatch.Key.SenderName)
                    : _registry.GetTopicSender(groupedDispatch.Key.SenderName);

                var paginatedMessages = groupedDispatch.Select(o => o.Message)
                    .Select((x, i) => new { Item = x, Index = i })
                    .GroupBy(x => x.Index / MaxMessagePerSend, x => x.Item);

                foreach (var pageMessages in paginatedMessages)
                {
                    await sender.SendAsync(pageMessages.Select(m => m).ToArray()).ConfigureAwait(false);
                }
            }
        }

        private Message CreateMessage(EventPublicationRegistration registration, object dto)
        {
            var result = _messageBodyParser.SerializeBody(dto);
            var message = MessageHelper.CreateMessage(result.ContentType, result.Body, registration.EventTypeId);
            foreach (var customizer in registration.OutgoingMessageCustomizers)
            {
                customizer?.Invoke(message, dto);
            }

            return message;
        }

        private class Dispatch
        {
            public Dispatch(Message message, EventPublicationRegistration registration)
            {
                this.Message = message;
                this.Registration = registration;
            }

            public Message Message { get; }
            public EventPublicationRegistration Registration { get; }
        }
    }
}
