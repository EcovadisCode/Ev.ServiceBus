﻿using System;
using System.Linq;

namespace Ev.ServiceBus.Reception
{
    public class DuplicateSubscriptionHandlerDeclarationException : Exception
    {
        public DuplicateSubscriptionHandlerDeclarationException(MessageReceptionRegistration[] duplicates)
        {
            Duplicates = duplicates;
            Message = "You cannot register the same handler Twice.\n"
                      + "Types at faults :\n"
                      + $"{string.Join("\n", duplicates.Select(o => $"{o.Options.ClientType} {o.Options.ResourceId} => {o.PayloadTypeId} => {o.HandlerType}"))}";
        }

        public MessageReceptionRegistration[] Duplicates { get; }
        public override string Message { get; }
    }
}
