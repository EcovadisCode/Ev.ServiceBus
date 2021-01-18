using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public static class ServiceCollectionHelpers
    {
        public static IServiceCollection OverrideClientFactories(this IServiceCollection services)
        {
            services.AddSingleton<FakeClientFactory>();
            services.AddSingleton<FakeTopicClientFactory>();
            services.AddSingleton<FakeSubscriptionClientFactory>();
            services.Replace(
                new ServiceDescriptor(
                    typeof(IClientFactory),
                    provider => provider.GetRequiredService<FakeClientFactory>(),
                    ServiceLifetime.Singleton));
            services.Replace(
                new ServiceDescriptor(
                    typeof(ITopicClientFactory),
                    provider => provider.GetRequiredService<FakeTopicClientFactory>(),
                    ServiceLifetime.Singleton));
            services.Replace(
                new ServiceDescriptor(
                    typeof(ISubscriptionClientFactory),
                    provider => provider.GetRequiredService<FakeSubscriptionClientFactory>(),
                    ServiceLifetime.Singleton));
            return services;
        }

        public static IServiceCollection OverrideQueueClientFactory(
            this IServiceCollection services,
            IClientFactory instance)
        {
            services.Replace(new ServiceDescriptor(typeof(IClientFactory), instance));
            return services;
        }

        public static IServiceCollection OverrideSubscriptionClientFactory(
            this IServiceCollection services,
            ISubscriptionClientFactory instance)
        {
            services.Replace(new ServiceDescriptor(typeof(ISubscriptionClientFactory), instance));
            return services;
        }

        public static IServiceCollection OverrideTopicClientFactory(
            this IServiceCollection services,
            ITopicClientFactory instance)
        {
            services.Replace(new ServiceDescriptor(typeof(ITopicClientFactory), instance));
            return services;
        }
    }
}
