namespace Ev.ServiceBus.Abstractions.MessageReception;

public interface IMessageMetadataAccessor
{
    public IMessageMetadata? Metadata { get; }
}

public class MessageMetadataAccessor : IMessageMetadataAccessor
{
    public void SetData(MessageContext context)
    {
        Metadata = new MessageMetadata(context.Message, context.CancellationToken);
    }

    public IMessageMetadata? Metadata { get; private set; }
}