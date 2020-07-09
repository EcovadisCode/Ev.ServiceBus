using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public static class ServiceCollectionHelpers
    {
        public static IServiceCollection OverrideClientFactories(this IServiceCollection services)
        {
            services.AddSingleton<FakeQueueClientFactory>();
            services.AddSingleton<FakeTopicClientFactory>();
            services.AddSingleton<FakeSubscriptionClientFactory>();
            services.Replace(
                new ServiceDescriptor(
                    typeof(IQueueClientFactory),
                    provider => provider.GetRequiredService<FakeQueueClientFactory>(),
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
            IQueueClientFactory instance)
        {
            services.Replace(new ServiceDescriptor(typeof(IQueueClientFactory), instance));
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
