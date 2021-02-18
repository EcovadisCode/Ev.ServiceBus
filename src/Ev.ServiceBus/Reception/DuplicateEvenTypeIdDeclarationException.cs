using System;
using System.Linq;

namespace Ev.ServiceBus.Reception
{
    public class DuplicateEvenTypeIdDeclarationException : Exception
    {
        public DuplicateEvenTypeIdDeclarationException(MessageReceptionRegistration[] duplicates)
        {
            Duplicates = duplicates;
            Message = "You cannot register the same EventTypeId twice for the same subscription.\n"
                      + "Duplicates at fault :\n"
                      + $"{string.Join("\n", duplicates.Select(o => $"{o.Options.ClientType} {o.Options.ResourceId} => {o.EventTypeId} => {o.HandlerType}"))}";
        }

        public MessageReceptionRegistration[] Duplicates { get; }
        public override string Message { get; }
    }
}
