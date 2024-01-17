using Ev.ServiceBus.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.UnitTests.Helpers;

public static class ServiceCollectionHelpers
{
    public static IServiceCollection OverrideClientFactory(this IServiceCollection services)
    {
        services.AddSingleton<FakeClientFactory>();
        services.Replace(
            new ServiceDescriptor(
                typeof(IClientFactory),
                provider => provider.GetRequiredService<FakeClientFactory>(),
                ServiceLifetime.Singleton));
        return services;
    }

    public static IServiceCollection OverrideClientFactory(
        this IServiceCollection services,
        IClientFactory instance)
    {
        services.Replace(new ServiceDescriptor(typeof(IClientFactory), instance));
        return services;
    }
}