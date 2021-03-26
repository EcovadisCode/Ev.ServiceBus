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

        /// <summary>
        /// the received message
        /// </summary>
        public Message Message { get; }

        /// <summary>
        /// The resource that received the message
        /// </summary>
        public IMessageReceiver Receiver { get; }

        /// <summary>
        /// The cancellation token for the whole execution of the message.
        /// Generally this token is cancelled when you are in 'peekLock' mode (this is the default mode)
        /// and the execution reached the maximum of time allowed to be processed and is abandoned.
        /// </summary>
        public CancellationToken Token { get; }
    }
}
