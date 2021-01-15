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
                provider)
        {
            _options = options;
        }

        internal IQueueClient? QueueClient { get; private set; }

        protected override (IMessageSender, MessageReceiver?) CreateClient(ConnectionSettings settings)
        {
            var factory = Provider.GetService<IClientFactory>();
            QueueClient = (IQueueClient) factory.Create(_options, settings);
            var sender = new MessageSender(QueueClient, _options.EntityPath, _options.ClientType);
            var receiver = new MessageReceiver(QueueClient, _options.EntityPath, _options.ClientType);
            RegisterMessageHandler(_options, receiver);
            return (sender, receiver);
        }
    }
}
