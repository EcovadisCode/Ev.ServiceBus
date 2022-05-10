using System;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionSucceededArgs : ExecutionBaseArgs
{
    public ExecutionSucceededArgs(
        ClientType clientType,
        string resourceId,
        Type messageHandlerType,
        ServiceBusReceivedMessage message,
        long executionDurationMilliseconds)
        : base(clientType, resourceId, messageHandlerType, message)
    {
        ExecutionDurationMilliseconds = executionDurationMilliseconds;
    }

    public long ExecutionDurationMilliseconds { get; }
}