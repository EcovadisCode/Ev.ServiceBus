using System;

namespace Ev.ServiceBus.Abstractions.Exceptions;

[Serializable]
public class BatchingFailedException : Exception
{
    public BatchingFailedException() { }
    public BatchingFailedException(Exception ex) : base("Batching failed", ex) { }
}
