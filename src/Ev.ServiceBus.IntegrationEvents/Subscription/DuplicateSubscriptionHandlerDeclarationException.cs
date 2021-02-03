﻿using System;
using System.Linq;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class DuplicateSubscriptionHandlerDeclarationException : Exception
    {
        public DuplicateSubscriptionHandlerDeclarationException(EventSubscriptionRegistration[] duplicates)
        {
            Duplicates = duplicates;
            Message = "You cannot register the same handler Twice.\n"
                      + "Types at faults :\n"
                      + $"{string.Join("\n", duplicates.Select(o => $"{o.ClientType} {o.ReceiverName} => {o.EventTypeId} => {o.HandlerType}"))}";
        }

        public EventSubscriptionRegistration[] Duplicates { get; }
        public override string Message { get; }
    }
}