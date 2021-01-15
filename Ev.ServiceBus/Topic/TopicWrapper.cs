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
            : base(options, parentOptions, provider)
        {
            _options = options;
        }

        internal ITopicClient? TopicClient { get; private set; }

        protected override (IMessageSender, MessageReceiver?) CreateClient(ConnectionSettings settings)
        {
            var factory = Provider.GetService<ITopicClientFactory>();
            TopicClient = (ITopicClient) factory.Create(_options, settings);
            var sender = new MessageSender(TopicClient, _options.EntityPath, _options.ClientType);
            return (sender, null);
        }
    }
}
