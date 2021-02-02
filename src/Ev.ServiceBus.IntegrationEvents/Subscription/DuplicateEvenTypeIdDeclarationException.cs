using System;
using System.Linq;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class DuplicateEvenTypeIdDeclarationException : Exception
    {
        public DuplicateEvenTypeIdDeclarationException(EventSubscriptionRegistration[] duplicates)
        {
            Duplicates = duplicates;
            Message = "You cannot register the same EventTypeId twice for the same subscription.\n"
                      + "Duplicates at fault :\n"
                      + $"{string.Join("\n", duplicates.Select(o => $"{o.ClientType} {o.ReceiverName} => {o.EventTypeId} => {o.HandlerType}"))}";
        }

        public EventSubscriptionRegistration[] Duplicates { get; }
        public override string Message { get; }
    }
}
