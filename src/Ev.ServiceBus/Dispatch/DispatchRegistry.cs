using System;
using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions.Exceptions;

namespace Ev.ServiceBus.Dispatch
{
    public class DispatchRegistry
    {
        private readonly MessageDispatchRegistration[] _registrations;

        public DispatchRegistry(
            IEnumerable<MessageDispatchRegistration> registrations)
        {
            _registrations = registrations.ToArray();

            var doubleRegistrations = _registrations.GroupBy(o => o).Where(o => o.Count() > 1).ToArray();
            if (doubleRegistrations.Any())
            {
                throw new MultiplePublicationRegistrationException(doubleRegistrations.Select(o => o.Key.ToString()).ToArray());
            }
        }

        public MessageDispatchRegistration[] GetRegistrations(Type messageType)
        {
            return _registrations
                .Where(o => o.PayloadType == messageType)
                .ToArray();
        }
    }
}
