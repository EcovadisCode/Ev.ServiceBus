namespace Ev.ServiceBus.Abstractions.MessageReception;

public interface IMessageMetadataAccessor
{
    public IMessageMetadata? Metadata { get; }
}