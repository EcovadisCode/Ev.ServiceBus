using System;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class EventSubscriptionRegistration
    {
        public string EventTypeId { get; }
        public Type EventType { get; }
        public Type HandlerType { get; }

        public EventSubscriptionRegistration(string eventTypeId, Type eventType, Type handlerType)
        {
            EventTypeId = eventTypeId;
            EventType = eventType;
            HandlerType = handlerType;
        }
    }
}
