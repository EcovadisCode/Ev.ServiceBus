using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public abstract class ExecutionBaseArgs
    {
        protected ExecutionBaseArgs(MessageContext context, Type messageHandlerType)
        {
            ClientType = context.ClientType;
            ResourceId = context.ResourceId;
            MessageHandlerType = messageHandlerType;
            MessageLabel = context.Message.Subject;
            MessageApplicationProperties = context.Message.ApplicationProperties.ToDictionary(pair => pair.Key, pair => pair.Value);
            ReceptionRegistration = context.ReceptionRegistration;
        }

        public MessageReceptionRegistration? ReceptionRegistration { get; }
        public ClientType ClientType { get; }
        public string ResourceId { get; }
        public Type MessageHandlerType { get; }
        public IDictionary<string, object> MessageApplicationProperties { get; }
        public string MessageLabel { get; }
    }
}
