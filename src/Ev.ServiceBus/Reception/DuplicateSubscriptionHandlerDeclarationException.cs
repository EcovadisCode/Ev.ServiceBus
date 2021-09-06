using System;
using System.Linq;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Reception
{
    public class DuplicateSubscriptionHandlerDeclarationException : Exception
    {
        public DuplicateSubscriptionHandlerDeclarationException(MessageReceptionRegistration[] duplicates)
        {

            Message = "You cannot register the same handler Twice.\n"
                      + "Types at faults :\n"
                      + $"{string.Join("\n", duplicates.Select(o => $"{o.Options.ClientType} {o.Options.ResourceId} => {o.PayloadTypeId} => {o.HandlerType}"))}";
        }

        public override string Message { get; }
    }
}
