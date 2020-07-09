using System.Collections.Generic;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus.Core;
using IMessageReceiver = Ev.ServiceBus.Abstractions.IMessageReceiver;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class MessageReceiver : IMessageReceiver
    {
        internal IReceiverClient Client { get; }

        public MessageReceiver(IReceiverClient client, string name, ClientType clientType)
        {
            Client = client;
            Name = name;
            ClientType = clientType;
        }

        public string Name { get; }
        public ClientType ClientType { get; }

        public async Task CompleteAsync(string lockToken)
        {
            await Client.CompleteAsync(lockToken);
        }

        public async Task AbandonAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
        {
            await Client.AbandonAsync(lockToken, propertiesToModify);
        }

        public async Task DeadLetterAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
        {
            await Client.DeadLetterAsync(lockToken, propertiesToModify);
        }

        public async Task DeadLetterAsync(
            string lockToken,
            string deadLetterReason,
            string deadLetterErrorDescription = null)
        {
            await Client.DeadLetterAsync(lockToken, deadLetterReason, deadLetterErrorDescription);
        }
    }
}
