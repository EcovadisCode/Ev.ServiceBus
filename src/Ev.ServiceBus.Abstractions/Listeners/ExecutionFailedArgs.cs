using System;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionFailedArgs : ExecutionBaseArgs
{
    public ExecutionFailedArgs(
        ClientType clientType,
        string resourceId,
        Type messageHandlerType,
        ServiceBusReceivedMessage message,
        Exception exception)
        : base(clientType, resourceId, messageHandlerType, message)
    {
        Exception = exception;
    }

    public Exception Exception { get; }
}