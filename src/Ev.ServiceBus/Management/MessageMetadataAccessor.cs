using System.Threading;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Management;

internal class MessageMetadataAccessor : IMessageMetadataAccessor
{
    private static readonly AsyncLocal<MessageMetadataHolder> MessageMetadataCurrent =
        new AsyncLocal<MessageMetadataHolder>();

    public IMessageMetadata? Metadata
    {
        get => MessageMetadataCurrent.Value?.Metadata;
        set
        {
            var holder = MessageMetadataCurrent.Value;
            if (holder != null)
            {
                holder.Metadata = null;
            }

            if (value != null)
            {
                MessageMetadataCurrent.Value = new MessageMetadataHolder { Metadata = value };
            }
        }
    }

    private class MessageMetadataHolder
    {
        public IMessageMetadata? Metadata;
    }
}