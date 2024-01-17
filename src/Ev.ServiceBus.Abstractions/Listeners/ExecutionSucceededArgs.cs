using System;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionSucceededArgs : ExecutionBaseArgs
{
    public ExecutionSucceededArgs(MessageContext context, long executionDurationMilliseconds)
        : base(context)
    {
        ExecutionDurationMilliseconds = executionDurationMilliseconds;
    }

    public long ExecutionDurationMilliseconds { get; }
}