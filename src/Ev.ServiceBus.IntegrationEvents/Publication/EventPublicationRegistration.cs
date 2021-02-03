using System;
using System.Collections.Generic;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class EventPublicationRegistration
    {
        public EventPublicationRegistration(
            string eventTypeId,
            Type eventType,
            ClientType clientType,
            string senderName,
            IList<Action<Message, object>> outgoingMessageCustomizers)
        {
            EventTypeId = eventTypeId;
            EventType = eventType;
            SenderName = senderName;
            ClientType = clientType;
            OutgoingMessageCustomizers = outgoingMessageCustomizers;
        }

        public string EventTypeId { get; }
        public Type EventType { get; }

        public string SenderName { get; }
        public ClientType ClientType { get; }
        public IList<Action<Message, object>> OutgoingMessageCustomizers { get; }

        public override bool Equals(object? obj)
        {
            var reg = obj as EventPublicationRegistration;
            return EventType == reg?.EventType && ClientType == reg?.ClientType && SenderName == reg?.SenderName;
        }

        public override int GetHashCode()
        {
            return EventType.GetHashCode() ^ ClientType.GetHashCode() ^ SenderName.GetHashCode();
        }

        public override string ToString()
        {
            return $"{EventType.FullName}|{ClientType}|{SenderName}";
        }
    }
}
