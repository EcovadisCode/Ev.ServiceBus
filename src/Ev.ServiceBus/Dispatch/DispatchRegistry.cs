using System;
using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch
{
    public class DispatchRegistry
    {
        private readonly Dictionary<Type, MessageDispatchRegistration[]> _registrations;

        public DispatchRegistry(
            IEnumerable<MessageDispatchRegistration> registrations)
        {
            _registrations = new Dictionary<Type, MessageDispatchRegistration[]>();

            var doubleRegistrations = registrations.GroupBy(o => o).Where(o => o.Count() > 1).ToArray();
            if (doubleRegistrations.Any())
            {
                throw new MultiplePublicationRegistrationException(doubleRegistrations.Select(o => o.Key.ToString()).ToArray());
            }

            foreach (var group in registrations.GroupBy(o => o.PayloadType))
            {
                _registrations.Add(group.Key, group.ToArray());
            }
        }

        public MessageDispatchRegistration[] GetRegistrations(Type messageType)
        {
            if (_registrations.TryGetValue(messageType, out var registrations))
            {
                return registrations;
            }

            throw new DispatchRegistrationNotFoundException(messageType);
        }
    }
}
