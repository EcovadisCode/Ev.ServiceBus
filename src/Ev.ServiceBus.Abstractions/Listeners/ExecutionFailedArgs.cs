using System;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public class ExecutionFailedArgs : ExecutionBaseArgs
    {
        public ExecutionFailedArgs(
            ClientType clientType,
            string resourceId,
            Type messageHandlerType,
            Message message,
            Exception exception)
            : base(clientType, resourceId, messageHandlerType, message)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
