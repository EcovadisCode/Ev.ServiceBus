using System;

namespace Ev.ServiceBus.Abstractions;

[Serializable]
public class SenderNotFoundException : Exception
{
    public SenderNotFoundException(string resourceId)
        : base(
            $"The '{resourceId}' you tried to retrieve was not found. "
            + $"Verify your configuration to make sure the resource is properly registered.")
    {
        ResourceId = resourceId;
    }

    public string ResourceId { get; }
}