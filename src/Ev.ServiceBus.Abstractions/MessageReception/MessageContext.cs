using System.Threading;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class MessageContext
    {
        public MessageContext(
            Message message,
            IMessageReceiver receiver,
            CancellationToken token)
        {
            Message = message;
            Token = token;
            Receiver = receiver;
        }

        public Message Message { get; }
        public IMessageReceiver Receiver { get; }
        public CancellationToken Token { get; }
    }
}
