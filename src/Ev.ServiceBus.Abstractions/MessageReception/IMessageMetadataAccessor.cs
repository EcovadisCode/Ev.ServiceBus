namespace Ev.ServiceBus.Abstractions.MessageReception;

public interface IMessageMetadataAccessor
{
    IMessageMetadata? Metadata { get; }
    void SetData(MessageContext context);
}