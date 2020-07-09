using System.Linq;
using System.Runtime.CompilerServices;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: InternalsVisibleTo("Ev.ServiceBus.UnitTests")]

namespace Ev.ServiceBus
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers basic services for using ServiceBus.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="enabled">If false, all interactions with servicebus (reception and sending) will be deactivated.</param>
        /// <param name="receiveMessages">If false, reception of messages will be deactivated.</param>
        /// <returns></returns>
        public static IServiceCollection AddServiceBus(
            this IServiceCollection services,
            bool enabled = true,
            bool receiveMessages = true)
        {
            services.TryAddSingleton<ServiceBusRegistry>();
            services.AddSingleton<IServiceBusRegistry, ServiceBusRegistry>(
                provider => provider.GetRequiredService<ServiceBusRegistry>());
            services.TryAddSingleton<ServiceBusEngine>();

            services.TryAddSingleton<IQueueClientFactory, QueueClientFactory>();
            services.TryAddSingleton<ITopicClientFactory, TopicClientFactory>();
            services.TryAddSingleton<ISubscriptionClientFactory, SubscriptionClientFactory>();

            services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.Enabled = enabled;
                    options.ReceiveMessages = receiveMessages;
                });

            if (services.Any(o => o.ImplementationType == typeof(ServiceBusHost)) == false)
            {
                services.AddHostedService<ServiceBusHost>();
            }

            return services;
        }
    }
}
