using System;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionFailedArgs : ExecutionBaseArgs
{
    public ExecutionFailedArgs(MessageContext context, Exception exception)
        : base(context)
    {
        Exception = exception;
    }

    public Exception Exception { get; }
}