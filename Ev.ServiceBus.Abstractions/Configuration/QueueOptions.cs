using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class QueueOptions : ReceiverOptions
    {
        private readonly IServiceCollection _serviceCollection;

        public QueueOptions(IServiceCollection serviceCollection, string queueName) : base(queueName, ClientType.Queue)
        {
            _serviceCollection = serviceCollection;
            QueueName = queueName;
        }

        public string QueueName { get; }

        public QueueOptions WithCustomMessageHandler<TMessageHandler>(Action<MessageHandlerOptions>? config = null)
            where TMessageHandler : class, IMessageHandler
        {
            _serviceCollection.TryAddScoped<TMessageHandler>();
            MessageHandlerType = typeof(TMessageHandler);
            MessageHandlerConfig = config;
            return this;
        }

        public QueueOptions WithCustomExceptionHandler<TExceptionHandler>()
            where TExceptionHandler : class, IExceptionHandler
        {
            _serviceCollection.TryAddScoped<TExceptionHandler>();
            ExceptionHandlerType = typeof(TExceptionHandler);
            return this;
        }
    }
}
