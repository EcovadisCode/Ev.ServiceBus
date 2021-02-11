using System;
using System.Collections.Generic;
using System.Linq;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class PublicationRegistry
    {
        private readonly MessageDispatchRegistration[] _registrations;

        public PublicationRegistry(
            IEnumerable<MessageDispatchRegistration> registrations)
        {
            _registrations = registrations.ToArray();

            var doubleRegistrations = _registrations.GroupBy(o => o).Where(o => o.Count() > 1).ToArray();
            if (doubleRegistrations.Any())
            {
                throw new MultiplePublicationRegistrationException(doubleRegistrations.Select(o => o.Key).ToArray());
            }
        }

        public MessageDispatchRegistration[] GetRegistrations(Type messageType)
        {
            return _registrations
                .Where(o => o.EventType == messageType)
                .ToArray();
        }
    }
}
