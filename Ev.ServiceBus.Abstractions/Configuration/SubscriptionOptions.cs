using System;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class SubscriptionOptions : ReceiverOptions
    {
        public SubscriptionOptions(string topicName, string subscriptionName)
            : base(topicName)
        {
            SubscriptionName = subscriptionName;
            TopicName = topicName;
        }

        public string SubscriptionName { get; }
        public string TopicName { get; }

        public SubscriptionOptions WithCustomMessageHandler<TMessageHandler>(
            Action<MessageHandlerOptions> config = null)
            where TMessageHandler : IMessageHandler
        {
            MessageHandlerType = typeof(TMessageHandler);
            MessageHandlerConfig = config;
            return this;
        }

        public SubscriptionOptions WithCustomExceptionHandler<TExceptionHandler>()
            where TExceptionHandler : IExceptionHandler
        {
            ExceptionHandlerType = typeof(TExceptionHandler);
            return this;
        }
    }
}
