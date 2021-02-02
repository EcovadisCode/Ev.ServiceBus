using System;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public abstract class EventPublicationRegistration
    {
        protected EventPublicationRegistration(string eventTypeId, Type eventType, Type handlerType)
        {
            HandlerType = handlerType;
            EventTypeId = eventTypeId;
            EventType = eventType;
        }

        public Type HandlerType { get; }
        public string EventTypeId { get; }
        public Type EventType { get; }

        public override bool Equals(object? obj)
        {
            var reg = obj as EventPublicationRegistration;
            return EventType == reg?.EventType;
        }

        public override int GetHashCode()
        {
            return EventType.GetHashCode();
        }

        public override string ToString()
        {
            return $"{EventType.FullName}";
        }
    }
}
