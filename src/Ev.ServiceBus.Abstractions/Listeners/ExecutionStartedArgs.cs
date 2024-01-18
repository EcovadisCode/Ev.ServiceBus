using System;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionStartedArgs : ExecutionBaseArgs
{
    public ExecutionStartedArgs(MessageContext context) : base(context)
    {

    }
}