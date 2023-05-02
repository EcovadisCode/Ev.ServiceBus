using System;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionStartedArgs : ExecutionBaseArgs
{
    public ExecutionStartedArgs(MessageContext context, Type messageHandlerType)
        : base(context, messageHandlerType)
    {
    }
}