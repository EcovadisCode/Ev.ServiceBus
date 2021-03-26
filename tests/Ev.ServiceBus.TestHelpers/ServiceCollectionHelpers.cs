using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.TestHelpers;
using Microsoft.Azure.ServiceBus;
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
                    typeof(IClientFactory<QueueOptions, IQueueClient>),
                    provider => provider.GetRequiredService<FakeQueueClientFactory>(),
                    ServiceLifetime.Singleton));
            services.Replace(
                new ServiceDescriptor(
                    typeof(IClientFactory<TopicOptions, ITopicClient>),
                    provider => provider.GetRequiredService<FakeTopicClientFactory>(),
                    ServiceLifetime.Singleton));
            services.Replace(
                new ServiceDescriptor(
                    typeof(IClientFactory<SubscriptionOptions, ISubscriptionClient>),
                    provider => provider.GetRequiredService<FakeSubscriptionClientFactory>(),
                    ServiceLifetime.Singleton));
            return services;
        }

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
