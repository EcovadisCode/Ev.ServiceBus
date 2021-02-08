using System;
using System.Collections.Generic;
using System.Linq;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class PublicationRegistry
    {
        private readonly EventPublicationRegistration[] _registrations;

        public PublicationRegistry(
            IEnumerable<EventPublicationRegistration> registrations)
        {
            _registrations = registrations.ToArray();

            var doubleRegistrations = _registrations.GroupBy(o => o).Where(o => o.Count() > 1).ToArray();
            if (doubleRegistrations.Any())
            {
                throw new MultiplePublicationRegistrationException(doubleRegistrations.Select(o => o.Key).ToArray());
            }
        }

        public EventPublicationRegistration[] GetRegistrations(Type messageType)
        {
            return _registrations
                .Where(o => o.EventType == messageType)
                .ToArray();
        }
    }
}
