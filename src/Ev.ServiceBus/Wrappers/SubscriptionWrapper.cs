using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class SubscriptionWrapper : ReceiverWrapper
    {
        private readonly SubscriptionOptions _options;

        public SubscriptionWrapper(
            SubscriptionOptions options,
            ServiceBusOptions parentOptions,
            IServiceProvider provider)
            : base(
                options,
                parentOptions,
                provider)
        {
            _options = options;
        }

        internal ISubscriptionClient? SubscriptionClient { get; private set; }

        protected override (IMessageSender, MessageReceiver?) CreateClient(ConnectionSettings settings)
        {
            var factory = Provider.GetService<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            SubscriptionClient = factory.Create(_options, settings);
            var receiver = new MessageReceiver(SubscriptionClient, _options.EntityPath, _options.ClientType);
            RegisterMessageHandler(_options, receiver);
            return (new DeactivatedSender(_options.EntityPath, _options.ClientType), receiver);
        }
    }
}
