using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class IntegrationEventDispatcher : IIntegrationEventPublisher, IIntegrationEventDispatcher
    {
        private readonly List<KeyValuePair<EventPublicationRegistration, object>> _eventsToSend;
        private readonly PublicationRegistry _registry;

        public IntegrationEventDispatcher(PublicationRegistry registry)
        {
            _registry = registry;
            _eventsToSend = new List<KeyValuePair<EventPublicationRegistration, object>>();
        }

        public async Task DispatchEvents()
        {
            if (_eventsToSend.Any())
            {
                foreach (var group in _eventsToSend.GroupBy(o => o.Key.HandlerType))
                {
                    var sender = _registry.GetSender(group.Key);
                    await sender.SendEvents(group.ToArray()).ConfigureAwait(false);
                }

                _eventsToSend.Clear();
            }
        }

        public void Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        {
            if (integrationEvent == null)
            {
                throw new ArgumentNullException(nameof(integrationEvent));
            }

            var registrations = _registry.GetRegistrations<TIntegrationEvent>();

            foreach (var eventPublicationRegistration in registrations)
            {
                _eventsToSend.Add(
                    new KeyValuePair<EventPublicationRegistration, object>(
                        eventPublicationRegistration,
                        integrationEvent));
            }
        }
    }
}
