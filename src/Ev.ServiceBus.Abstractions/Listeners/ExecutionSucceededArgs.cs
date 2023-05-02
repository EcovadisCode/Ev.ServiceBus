using System;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions;

public class ExecutionSucceededArgs : ExecutionBaseArgs
{
    public ExecutionSucceededArgs(MessageContext context, Type messageHandlerType, long executionDurationMilliseconds)
        : base(context, messageHandlerType)
    {
        ExecutionDurationMilliseconds = executionDurationMilliseconds;
    }

    public long ExecutionDurationMilliseconds { get; }
}