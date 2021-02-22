using System;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Reception
{
    public class MessageReceptionRegistration
    {
        public MessageReceptionRegistration(ClientOptions clientOptions, Type payloadType, Type handlerType)
        {
            Options = clientOptions;
            PayloadType = payloadType;
            HandlerType = handlerType;
            PayloadTypeId = PayloadType.Name;
        }

        public ClientOptions Options { get; }
        public Type PayloadType { get; }
        public Type HandlerType { get; }
        public string PayloadTypeId { get; private set; }

        public MessageReceptionRegistration CustomizePayloadTypeId(string payloadTypeId)
        {
            if (payloadTypeId == null)
            {
                throw new ArgumentNullException(nameof(payloadTypeId));
            }

            PayloadTypeId = payloadTypeId;
            return this;
        }
    }
}
