using System;
using System.Linq;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class DuplicateSubscriptionHandlerDeclarationException : Exception
    {
        public Type[] Types { get; }

        public DuplicateSubscriptionHandlerDeclarationException(Type[] types)
            : base("You cannot register the same handler Twice.\n"
                   + "Types at faults :\n"
                   + $"{string.Join("\n", types.Select(o => o.FullName))}")
        {
            Types = types;
        }
    }
}
