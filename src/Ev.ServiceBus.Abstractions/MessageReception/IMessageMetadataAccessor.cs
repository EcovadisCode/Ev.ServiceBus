using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions.MessageReception
{
    public interface IMessageMetadataAccessor
    {
        public IMessageMetadata? Metadata { get; }
    }

    public class MessageMetadataAccessor : IMessageMetadataAccessor
    {
        internal void SetData(Message message, CancellationToken token)
        {
            Metadata = new MessageMetadata(message, token);
        }

        public IMessageMetadata? Metadata { get; private set; }
    }
}
