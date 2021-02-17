using System;
using Microsoft.Azure.ServiceBus;
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

        public Type? MessageHandlerType { get; internal set; }
        public Action<MessageHandlerOptions>? MessageHandlerConfig { get; internal set; }
        public Type? ExceptionHandlerType { get; internal set; }

        public void WithCustomMessageHandler<TMessageHandler>(Action<MessageHandlerOptions>? config = null)
            where TMessageHandler : class, IMessageHandler
        {
            _services.TryAddScoped<TMessageHandler>();
            MessageHandlerType = typeof(TMessageHandler);
            MessageHandlerConfig = config;
        }

        public void WithCustomExceptionHandler<TExceptionHandler>()
            where TExceptionHandler : class, IExceptionHandler
        {
            _services.TryAddScoped<TExceptionHandler>();
            ExceptionHandlerType = typeof(TExceptionHandler);
        }
    }
}
