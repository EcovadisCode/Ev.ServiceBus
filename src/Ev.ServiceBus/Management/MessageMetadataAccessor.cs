using System.Threading;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Management;

internal class MessageMetadataAccessor : IMessageMetadataAccessor
{
    public IMessageMetadata? Metadata { get; private set; }

    public void SetData(MessageContext context)
    {
        Metadata = new MessageMetadata(context.Message, context.CancellationToken);
    }
}