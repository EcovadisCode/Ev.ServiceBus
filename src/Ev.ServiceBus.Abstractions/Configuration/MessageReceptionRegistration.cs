using System;
using System.Reflection;

namespace Ev.ServiceBus.Abstractions
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

        /// <summary>
        /// Settings of the underlying resource that will receive the messages.
        /// </summary>
        public ClientOptions Options { get; }

        /// <summary>
        /// The type the receiving message wil be deserialized into.
        /// </summary>
        public Type PayloadType { get; }

        /// <summary>
        /// The class that will be resolved to process the incoming message.
        /// </summary>
        public Type HandlerType { get; }

        /// <summary>
        /// The unique identifier of this payload's type.
        /// </summary>
        public string PayloadTypeId { get; private set; }

        /// <summary>
        /// Sets the PayloadTypeId (by default it will take the <see cref="MemberInfo.Name"/> of the payload <see cref="Type"/> object)
        /// </summary>
        /// <param name="payloadTypeId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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
