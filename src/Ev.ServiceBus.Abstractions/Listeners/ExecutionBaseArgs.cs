using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public abstract class ExecutionBaseArgs
    {
        protected ExecutionBaseArgs(
            ClientType clientType,
            string resourceId,
            Type messageHandlerType,
            ServiceBusReceivedMessage message)
        {
            ClientType = clientType;
            ResourceId = resourceId;
            MessageHandlerType = messageHandlerType;
            MessageLabel = message.Subject;
            MessageApplicationProperties = message.ApplicationProperties.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public ClientType ClientType { get; }
        public string ResourceId { get; }
        public Type MessageHandlerType { get; }
        public IDictionary<string, object> MessageApplicationProperties { get; }
        public string MessageLabel { get; }
    }
}
