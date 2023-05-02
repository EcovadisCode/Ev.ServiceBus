using System.Threading;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Reception;

namespace Ev.ServiceBus.Management;

internal class MessageMetadataAccessor : IMessageMetadataAccessor
{
    public IMessageMetadata? Metadata { get; private set; }

    public void SetData(MessageContext context)
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