using System;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public class ExecutionSucceededArgs : BaseArgs
    {
        public ExecutionSucceededArgs(
            ClientType clientType,
            string resourceId,
            Type messageHandlerType,
            Message message,
            long executionDurationMilliseconds)
            : base(clientType, resourceId, messageHandlerType, message)
        {
            ExecutionDurationMilliseconds = executionDurationMilliseconds;
        }

        public long ExecutionDurationMilliseconds { get; }
    }
}
