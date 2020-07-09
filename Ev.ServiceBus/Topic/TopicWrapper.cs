using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public class TopicWrapper : BaseWrapper
    {
        private readonly TopicOptions _options;

        public TopicWrapper(
            TopicOptions options,
            ServiceBusOptions parentOptions,
            IServiceProvider provider)
            : base(parentOptions, provider, options.TopicName)
        {
            _options = options;
        }

        internal ITopicClient TopicClient { get; private set; }

        protected override (IMessageSender, MessageReceiver, IClientEntity) CreateClient()
        {
            if (ParentOptions.Enabled == false)
            {
                return (new DeactivatedSender(Name, ClientType.Topic), null, null);
            }
            var factory = Provider.GetService<ITopicClientFactory>();
            TopicClient = factory.Create(_options);
            var sender = new MessageSender(TopicClient, Name, ClientType.Topic);
            return (sender, null, TopicClient);
        }
    }
}
