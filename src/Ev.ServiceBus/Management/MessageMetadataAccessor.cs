using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Management;

public class MessageMetadataAccessor : IMessageMetadataAccessor
{
    public IMessageMetadata? Metadata { get; private set; }

    internal void SetData(MessageContext context)
    {
        if (context.SessionArgs != null)
        {
            Metadata = new MessageMetadata(context.Message, context.SessionArgs, context.CancellationToken);
        }
        else
        {
            Metadata = new MessageMetadata(context.Message, context.Args!, context.CancellationToken);
        }
    }
}