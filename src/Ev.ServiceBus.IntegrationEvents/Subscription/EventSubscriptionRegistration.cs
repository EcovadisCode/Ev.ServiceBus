using System;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class EventSubscriptionRegistration
    {
        public EventSubscriptionRegistration(
            string eventTypeId,
            Type eventType,
            Type handlerType,
            ClientType clientType,
            string receiverName)
        {
            EventTypeId = eventTypeId;
            EventType = eventType;
            HandlerType = handlerType;
            ClientType = clientType;
            ReceiverName = receiverName;
        }

        public string EventTypeId { get; }
        public Type EventType { get; }
        public Type HandlerType { get; }

        public ClientType ClientType { get; }
        public string ReceiverName { get; }
    }
}
