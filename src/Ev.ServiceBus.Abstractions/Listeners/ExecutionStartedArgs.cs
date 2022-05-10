using System;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionStartedArgs : ExecutionBaseArgs
{
    public ExecutionStartedArgs(
        ClientType clientType,
        string resourceId,
        Type messageHandlerType,
        ServiceBusReceivedMessage message)
        : base(clientType, resourceId, messageHandlerType, message)
    {
    }
}