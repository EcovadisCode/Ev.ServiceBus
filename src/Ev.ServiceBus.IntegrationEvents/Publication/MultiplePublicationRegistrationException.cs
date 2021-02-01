using System;
using System.Collections.Generic;
using System.Linq;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class MultiplePublicationRegistrationException : Exception
    {
        public EventPublicationRegistration[] Registrations { get; }

        public MultiplePublicationRegistrationException(IReadOnlyList<EventPublicationRegistration> registrations)
            : base($"You can't register the same contract more than once.\n"
                   + $"Registrations at fault : \n"
                   + $"{string.Join("\n", registrations)}")
        {
            Registrations = registrations.ToArray();
        }
    }
}