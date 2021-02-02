using System;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class ServiceBusEventSubscriptionRegistration : EventSubscriptionRegistration
    {
        public ClientType ClientType { get; }
        public string ReceiverName { get; }

        public ServiceBusEventSubscriptionRegistration(
            string eventTypeId,
            Type eventType,
            Type handlerType,
            ClientType clientType,
            string receiverName)
            : base(eventTypeId, eventType, handlerType)
        {
            ClientType = clientType;
            ReceiverName = receiverName;
        }
    }
}
