using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers
{
    public static class ServiceCollectionHelpers
    {
        public static IServiceCollection OverrideClientFactory<TOptions, TClient>(
            this IServiceCollection services,
            IClientFactory<TOptions, TClient> instance)
            where TOptions : ClientOptions where TClient : IClientEntity
        {
            services.Replace(new ServiceDescriptor(typeof(IClientFactory<TOptions, TClient>), instance));
            return services;
        }
    }
}
