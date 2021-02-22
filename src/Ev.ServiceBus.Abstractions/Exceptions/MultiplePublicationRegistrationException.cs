using System;
using System.Collections.Generic;

namespace Ev.ServiceBus.Abstractions.Exceptions
{
    [Serializable]
    public class MultiplePublicationRegistrationException : Exception
    {
        public MultiplePublicationRegistrationException(IReadOnlyList<string> registrationIds)
        {
            Message = $"You can't register the same contract more than once.\n"
                      + $"Registrations at fault : \n"
                      + $"{string.Join("\n", registrationIds)}";
        }

        public override string Message { get; }
    }
}