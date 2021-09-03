using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public class MessageDispatchRegistration
    {
        private readonly List<Action<Message, object>> _outgoingCustomizers;

        public MessageDispatchRegistration(
            ClientOptions options,
            Type payloadType)
        {
            Options = options;
            PayloadTypeId = payloadType.Name;
            PayloadType = payloadType;
            _outgoingCustomizers = new List<Action<Message, object>>();
        }

        /// <summary>
        /// Settings of the underlying resource that will receive the messages.
        /// </summary>
        internal ClientOptions Options { get; }

        /// <summary>
        /// Callbacks called each time a message is sent.
        /// </summary>
        internal IReadOnlyList<Action<Message, object>> OutgoingMessageCustomizers => _outgoingCustomizers;

        /// <summary>
        /// The unique identifier of this payload's type.
        /// </summary>
        public string PayloadTypeId { get; private set; }

        /// <summary>
        /// The type the receiving message wil be deserialized into.
        /// </summary>
        public Type PayloadType { get; }

        /// <summary>
        /// Sets the PayloadTypeId (by default it will take the <see cref="MemberInfo.Name"/> of the payload <see cref="Type"/> object)
        /// </summary>
        /// <param name="payloadId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public MessageDispatchRegistration CustomizePayloadTypeId(string payloadId)
        {
            if (payloadId == null)
            {
                throw new ArgumentNullException(nameof(payloadId));
            }

            PayloadTypeId = payloadId;
            return this;
        }

        /// <summary>
        /// This method give you the possibility to customize outgoing messages right before they are dispatched.
        /// </summary>
        /// <param name="messageCustomizer"></param>
        /// <returns></returns>
        public MessageDispatchRegistration CustomizeOutgoingMessage(Action<Message, object> messageCustomizer)
        {
            _outgoingCustomizers.Add(messageCustomizer);
            return this;
        }

        public override bool Equals(object? obj)
        {
            var reg = obj as MessageDispatchRegistration;
            return PayloadType == reg?.PayloadType && Options.ClientType == reg?.Options.ClientType && Options.ResourceId == reg?.Options.ResourceId;
        }

        public override int GetHashCode()
        {
            return PayloadType.GetHashCode() ^ Options.ClientType.GetHashCode() ^ Options.ResourceId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{PayloadType.FullName}|{Options.ClientType}|{Options.ResourceId}";
        }
    }
}
