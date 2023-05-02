using System;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionFailedArgs : ExecutionBaseArgs
{
    public ExecutionFailedArgs(MessageContext context, Type messageHandlerType, Exception exception)
        : base(context, messageHandlerType)
    {
        Exception = exception;
    }

    public Exception Exception { get; }
}