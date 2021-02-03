﻿using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class ServiceBusEventSubscriptionRegistry
    {
        private readonly Dictionary<string, EventSubscriptionRegistration[]> _registrations;

        public ServiceBusEventSubscriptionRegistry(IEnumerable<EventSubscriptionRegistration> registrations)
        {
            var regs = registrations.ToArray();

            var duplicatedHandlers = regs.GroupBy(o => new { o.ClientType, o.ReceiverName, o.HandlerType }).Where(o => o.Count() > 1).ToArray();
            if (duplicatedHandlers.Any())
            {
                throw new DuplicateSubscriptionHandlerDeclarationException(duplicatedHandlers.SelectMany(o => o).ToArray());
            }

            var duplicateEvenTypeIds = regs.GroupBy(o => new {o.ClientType, o.ReceiverName, o.EventTypeId}).Where(o => o.Count() > 1).ToArray();
            if (duplicateEvenTypeIds.Any())
            {
                throw new DuplicateEvenTypeIdDeclarationException(duplicateEvenTypeIds.SelectMany(o => o).ToArray());
            }

            _registrations = regs
                .GroupBy(o => new { o.ClientType, o.ReceiverName, o.EventTypeId })
                .ToDictionary(
                    o => ComputeKey(o.Key.EventTypeId, o.Key.ReceiverName, o.Key.ClientType),
                    o => o.ToArray());
        }

        private string ComputeKey(string eventTypeId, string receiverName, ClientType clientType)
        {
            return $"{clientType}|{receiverName}|{eventTypeId}";
        }

        public EventSubscriptionRegistration? GetRegistration(string eventTypeId, string receiverName, ClientType clientType)
        {
            if (_registrations.TryGetValue(ComputeKey(eventTypeId, receiverName, clientType), out var registrations))
            {
                return registrations.First();
            }

            return null;
        }

    }
}