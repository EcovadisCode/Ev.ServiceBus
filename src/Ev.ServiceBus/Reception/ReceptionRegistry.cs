using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Reception
{
    public class ReceptionRegistry
    {
        private readonly Dictionary<string, MessageReceptionRegistration> _registrations;

        public ReceptionRegistry(IEnumerable<MessageReceptionRegistration> registrations)
        {
            var regs = registrations.ToArray();

            var duplicatedHandlers = regs.GroupBy(o => new { o.Options.ClientType,
                o.Options.ResourceId, o.HandlerType }).Where(o => o.Count() > 1).ToArray();
            if (duplicatedHandlers.Any())
            {
                throw new DuplicateSubscriptionHandlerDeclarationException(duplicatedHandlers.SelectMany(o => o).ToArray());
            }

            var duplicateEvenTypeIds = regs.GroupBy(o => new {o.Options.ClientType,
                o.Options.ResourceId, EventTypeId = o.PayloadTypeId}).Where(o => o.Count() > 1).ToArray();
            if (duplicateEvenTypeIds.Any())
            {
                throw new DuplicateEvenTypeIdDeclarationException(duplicateEvenTypeIds.SelectMany(o => o).ToArray());
            }

            _registrations = regs
                .ToDictionary(
                    o => ComputeKey(o.PayloadTypeId, o.Options.ResourceId, o.Options.ClientType),
                    o => o);
        }

        private string ComputeKey(string payloadTypeId, string receiverName, ClientType clientType)
        {
            return $"{clientType}|{receiverName}|{payloadTypeId}";
        }

        public MessageReceptionRegistration? GetRegistration(string payloadTypeId, string receiverName, ClientType clientType)
        {
            if (_registrations.TryGetValue(ComputeKey(payloadTypeId, receiverName, clientType), out var registrations))
            {
                return registrations;
            }

            return null;
        }

    }
}
