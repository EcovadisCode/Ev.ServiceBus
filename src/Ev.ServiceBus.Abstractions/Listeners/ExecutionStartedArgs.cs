using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public class ExecutionStartedArgs : ExecutionBaseArgs
    {
        public ExecutionStartedArgs(
            ClientType clientType,
            string resourceId,
            Type messageHandlerType,
            Message message)
            : base(clientType, resourceId, messageHandlerType, message)
        {
        }
    }
}
