using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public abstract class ExecutionBaseArgs
    {
        protected ExecutionBaseArgs(
            ClientType clientType,
            string resourceId,
            Type messageHandlerType,
            Message message)
        {
            ClientType = clientType;
            ResourceId = resourceId;
            MessageHandlerType = messageHandlerType;
            MessageLabel = message.Label;
            MessageUserProperties = message.UserProperties.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public ClientType ClientType { get; }
        public string ResourceId { get; }
        public Type MessageHandlerType { get; }
        public IDictionary<string, object> MessageUserProperties { get; }
        public string MessageLabel { get; }
    }
}
