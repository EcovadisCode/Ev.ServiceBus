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
            Type eventType)
        {
            Options = options;
            EventTypeId = eventType.Name;
            EventType = eventType;
            _outgoingCustomizers = new List<Action<Message, object>>();
        }

        internal ClientOptions Options { get; }
        internal IReadOnlyList<Action<Message, object>> OutgoingMessageCustomizers => _outgoingCustomizers;
        public string EventTypeId { get; private set; }
        public Type EventType { get; }

        public MessageDispatchRegistration CustomizeEventTypeId(string eventTypeId)
        {
            if (eventTypeId == null)
            {
                throw new ArgumentNullException(nameof(eventTypeId));
            }

            EventTypeId = eventTypeId;
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
            return EventType == reg?.EventType && Options.ClientType == reg?.Options.ClientType && Options.ResourceId == reg?.Options.ResourceId;
        }

        public override int GetHashCode()
        {
            return EventType.GetHashCode() ^ Options.ClientType.GetHashCode() ^ Options.ResourceId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{EventType.FullName}|{Options.ClientType}|{Options.ResourceId}";
        }
    }
}
