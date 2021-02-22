using System;
using System.Collections.Generic;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Dispatch
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

        internal ClientOptions Options { get; }
        internal IReadOnlyList<Action<Message, object>> OutgoingMessageCustomizers => _outgoingCustomizers;
        public string PayloadTypeId { get; private set; }
        public Type PayloadType { get; }

        public MessageDispatchRegistration CustomizePayloadTypeId(string payloadId)
        {
            if (payloadId == null)
            {
                throw new ArgumentNullException(nameof(payloadId));
            }

            PayloadTypeId = payloadId;
            return this;
        }

        public MessageDispatchRegistration CustomizeOutgoingMessage(
            Action<Message, object> messageCustomizer)
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
