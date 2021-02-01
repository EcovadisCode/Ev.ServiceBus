using System;
using System.Collections.Generic;
using System.Linq;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class PublicationRegistry
    {
        private readonly EventPublicationRegistration[] _registrations;
        private readonly IIntegrationEventSender[] _senders;

        public PublicationRegistry(
            IEnumerable<EventPublicationRegistration> registrations,
            IEnumerable<IIntegrationEventSender> senders)
        {
            _senders = senders.ToArray();
            _registrations = registrations.ToArray();

            var doubleRegistrations = _registrations.GroupBy(o => o).Where(o => o.Count() > 1).ToArray();
            if (doubleRegistrations.Any())
            {
                throw new MultiplePublicationRegistrationException(doubleRegistrations.Select(o => o.Key).ToArray());
            }
        }

        public EventPublicationRegistration[] GetRegistrations<TIntegrationEvent>()
        {
            return _registrations
                .Where(o => o.EventType == typeof(TIntegrationEvent))
                .ToArray();
        }

        public IIntegrationEventSender GetSender(Type senderType)
        {
            return _senders.First(o => o.GetType() == senderType);
        }
    }
}
