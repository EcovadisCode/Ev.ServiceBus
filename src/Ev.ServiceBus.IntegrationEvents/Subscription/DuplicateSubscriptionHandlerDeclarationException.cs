using System;
using System.Linq;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class DuplicateSubscriptionHandlerDeclarationException : Exception
    {
        public DuplicateSubscriptionHandlerDeclarationException(MessageReceptionRegistration[] duplicates)
        {
            Duplicates = duplicates;
            Message = "You cannot register the same handler Twice.\n"
                      + "Types at faults :\n"
                      + $"{string.Join("\n", duplicates.Select(o => $"{o.Options.ClientType} {o.Options.ResourceId} => {o.EventTypeId} => {o.HandlerType}"))}";
        }

        public MessageReceptionRegistration[] Duplicates { get; }
        public override string Message { get; }
    }
}
