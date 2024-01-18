using System;

namespace Ev.ServiceBus.Abstractions;

[Serializable]
public class SenderNotFoundException : Exception
{
    public SenderNotFoundException(ClientType clientType, string topicName)
        : base(
            $"The {clientType.ToString()} '{topicName}' you tried to retrieve was not found. "
            + $"Verify your configuration to make sure the {clientType.ToString()} is properly registered.")
    {
        TopicName = topicName;
    }

    public string TopicName { get; }
}