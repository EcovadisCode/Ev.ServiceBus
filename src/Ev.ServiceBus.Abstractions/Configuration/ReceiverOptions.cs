using System;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public abstract class ReceiverOptions : ClientOptions, IMessageReceiverOptions
    {
        private readonly IServiceCollection _services;

        protected ReceiverOptions(IServiceCollection services, string resourceId, ClientType clientType, bool strictMode)
            : base(resourceId, clientType, strictMode)
        {
            _services = services;
        }

        /// <inheritdoc />
        public Type? MessageHandlerType { get; private set; }

        /// <inheritdoc />
        public Action<ServiceBusProcessorOptions>? ServiceBusProcessorOptions { get; private set; }

        /// <inheritdoc />
        public Type? ExceptionHandlerType { get; private set; }

        public Action<ServiceBusSessionProcessorOptions>? SessionProcessorOptions { get; private set; }

        /// <summary>
        /// Defines a message handler for the current receiver.
        /// </summary>
        /// <param name="config"></param>
        /// <typeparam name="TMessageHandler"></typeparam>
        public void WithCustomMessageHandler<TMessageHandler>(Action<ServiceBusProcessorOptions> config)
            where TMessageHandler : class, IMessageHandler
        {
            _services.TryAddScoped<TMessageHandler>();
            MessageHandlerType = typeof(TMessageHandler);
            ServiceBusProcessorOptions = config;
        }

        /// <summary>
        /// Defines an exception handler for the current receiver.
        /// </summary>
        /// <typeparam name="TExceptionHandler"></typeparam>
        public void WithCustomExceptionHandler<TExceptionHandler>()
            where TExceptionHandler : class, IExceptionHandler
        {
            _services.TryAddScoped<TExceptionHandler>();
            ExceptionHandlerType = typeof(TExceptionHandler);
        }

        /// <summary>
        /// Activates session handling for the current receiver.
        /// </summary>
        public void EnableSessionHandling(Action<ServiceBusSessionProcessorOptions> config)
        {
            SessionProcessorOptions = config;
        }
    }
}
