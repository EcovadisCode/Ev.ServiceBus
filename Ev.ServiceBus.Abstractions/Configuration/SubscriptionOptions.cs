using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class SubscriptionOptions : ReceiverOptions
    {
        private readonly IServiceCollection _serviceCollection;

        public SubscriptionOptions(IServiceCollection serviceCollection, string topicName, string subscriptionName)
            : base(EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName), ClientType.Subscription)
        {
            _serviceCollection = serviceCollection;
            SubscriptionName = subscriptionName;
            TopicName = topicName;
        }

        public string SubscriptionName { get; }
        public string TopicName { get; }

        public SubscriptionOptions WithCustomMessageHandler<TMessageHandler>(
            Action<MessageHandlerOptions>? config = null)
            where TMessageHandler : class, IMessageHandler
        {
            _serviceCollection.TryAddScoped<TMessageHandler>();
            MessageHandlerType = typeof(TMessageHandler);
            MessageHandlerConfig = config;
            return this;
        }

        public SubscriptionOptions WithCustomExceptionHandler<TExceptionHandler>()
            where TExceptionHandler : class, IExceptionHandler
        {
            _serviceCollection.TryAddScoped<TExceptionHandler>();
            ExceptionHandlerType = typeof(TExceptionHandler);
            return this;
        }
    }
}
