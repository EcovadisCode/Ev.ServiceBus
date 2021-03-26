using System;
using System.Linq;

namespace Ev.ServiceBus.Reception
{
    public class DuplicateEvenTypeIdDeclarationException : Exception
    {
        public DuplicateEvenTypeIdDeclarationException(MessageReceptionRegistration[] duplicates)
        {
            Duplicates = duplicates;
            Message = "You cannot register the same PayloadTypeId twice for the same subscription.\n"
                      + "Duplicates at fault :\n"
                      + $"{string.Join("\n", duplicates.Select(o => $"{o.Options.ClientType} {o.Options.ResourceId} => {o.PayloadTypeId} => {o.HandlerType}"))}";
        }

        public MessageReceptionRegistration[] Duplicates { get; }
        public override string Message { get; }
    }
}
