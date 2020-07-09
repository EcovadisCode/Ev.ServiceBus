using System;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureServiceBus(this IServiceCollection services, Action<ServiceBusOptions> config)
        {
            services.Configure(config);
            return services;
        }
    }
}
