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
                provider,
                EntityNameHelper.FormatSubscriptionPath(options.TopicName, options.SubscriptionName))
        {
            _options = options;
        }

        internal ISubscriptionClient SubscriptionClient { get; private set; }

        protected override (IMessageSender, MessageReceiver, IClientEntity) CreateClient()
        {
            if (ParentOptions.Enabled == false)
            {
                return (null, null, null);
            }
            var factory = Provider.GetService<ISubscriptionClientFactory>();
            SubscriptionClient = factory.Create(_options);
            var receiver = new MessageReceiver(SubscriptionClient, Name, ClientType.Subscription);
            RegisterMessageHandler(_options, receiver);
            return (null, receiver, SubscriptionClient);
        }
    }
}
