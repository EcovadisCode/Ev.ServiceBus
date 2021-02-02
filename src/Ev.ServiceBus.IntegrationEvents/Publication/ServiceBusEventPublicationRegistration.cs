using System;
using System.Collections.Generic;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class ServiceBusEventPublicationRegistration : EventPublicationRegistration
    {
        public ServiceBusEventPublicationRegistration(
            string eventTypeId,
            Type eventType,
            ClientType clientType,
            string senderName,
            IList<Action<Message, object>> outgoingMessageCustomizers)
            : base(eventTypeId, eventType, typeof(ServiceBusIntegrationEventSender))
        {
            SenderName = senderName;
            ClientType = clientType;
            OutgoingMessageCustomizers = outgoingMessageCustomizers;
        }

        public string SenderName { get; }
        public ClientType ClientType { get; }
        public IList<Action<Message, object>> OutgoingMessageCustomizers { get; }

        public override bool Equals(object? obj)
        {
            var reg = obj as ServiceBusEventPublicationRegistration;
            return base.Equals(obj) && ClientType == reg?.ClientType && SenderName == reg?.SenderName;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ ClientType.GetHashCode() ^ SenderName.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString() + $"|{ClientType}|{SenderName}";
        }
    }
}
