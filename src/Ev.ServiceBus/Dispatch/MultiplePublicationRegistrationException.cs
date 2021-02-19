using System;
using System.Collections.Generic;
using System.Linq;

namespace Ev.ServiceBus.Dispatch
{
    public class MultiplePublicationRegistrationException : Exception
    {
        public MessageDispatchRegistration[] Registrations { get; }

        public MultiplePublicationRegistrationException(IReadOnlyList<MessageDispatchRegistration> registrations)
            : base($"You can't register the same contract more than once.\n"
                   + $"Registrations at fault : \n"
                   + $"{string.Join("\n", registrations)}")
        {
            Registrations = registrations.ToArray();
        }
    }
}