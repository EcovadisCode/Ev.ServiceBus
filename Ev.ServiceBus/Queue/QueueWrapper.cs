using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class QueueWrapper : ReceiverWrapper
    {
        private readonly QueueOptions _options;

        public QueueWrapper(QueueOptions options, ServiceBusOptions parentOptions, IServiceProvider provider)
            : base(
                options,
                parentOptions,
                provider,
                options.QueueName)
        {
            _options = options;
        }

        internal IQueueClient QueueClient { get; private set; }

        protected override (IMessageSender, MessageReceiver, IClientEntity) CreateClient()
        {
            if (ParentOptions.Enabled == false)
            {
                return (new DeactivatedSender(Name, ClientType.Queue), null, null);
            }
            var factory = Provider.GetService<IQueueClientFactory>();
            QueueClient = factory.Create(_options);
            var sender = new MessageSender(QueueClient, Name, ClientType.Queue);
            var receiver = new MessageReceiver(QueueClient, Name, ClientType.Queue);
            RegisterMessageHandler(_options, receiver);
            return (sender, receiver, QueueClient);
        }
    }
}
