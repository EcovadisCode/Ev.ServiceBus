using System;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Reception
{
    public class MessageReceptionRegistration
    {
        public MessageReceptionRegistration(ClientOptions clientOptions, Type receptionModelType, Type handlerType)
        {
            Options = clientOptions;
            ReceptionModelType = receptionModelType;
            HandlerType = handlerType;
            EventTypeId = ReceptionModelType.Name;
        }

        public ClientOptions Options { get; }
        public Type ReceptionModelType { get; }
        public Type HandlerType { get; }
        public string EventTypeId { get; private set; }

        public MessageReceptionRegistration CustomizeEventTypeId(string eventTypeId)
        {
            if (eventTypeId == null)
            {
                throw new ArgumentNullException(nameof(eventTypeId));
            }

            EventTypeId = eventTypeId;
            return this;
        }
    }
}
