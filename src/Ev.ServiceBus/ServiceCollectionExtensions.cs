using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
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
        /// <param name="config">Lambda expression to configure Service Bus</param>
        /// <returns></returns>
        public static IServiceCollection AddServiceBus(
            this IServiceCollection services,
            Action<ServiceBusSettings> config)
        {
            RegisterBaseServices(services);

            services.Configure<ServiceBusOptions>(
                options =>
                {
                    config(options.Settings);
                });

            return services;
        }

        private static void RegisterBaseServices(IServiceCollection services)
        {
            services.TryAddSingleton<ServiceBusRegistry>();
            if (services.Any(o => o.ServiceType == typeof(IServiceBusRegistry)) == false)
            {
                services.AddSingleton<IServiceBusRegistry, ServiceBusRegistry>(
                    provider => provider.GetRequiredService<ServiceBusRegistry>());
            }
            services.TryAddSingleton<ServiceBusEngine>();

            services.TryAddSingleton<IClientFactory<QueueOptions, IQueueClient>, QueueClientFactory>();
            services.TryAddSingleton<IClientFactory<TopicOptions, ITopicClient>, TopicClientFactory>();
            services.TryAddSingleton<IClientFactory<SubscriptionOptions, ISubscriptionClient>, SubscriptionClientFactory>();

            if (services.Any(o => o.ImplementationType == typeof(ServiceBusHost)) == false)
            {
                services.AddHostedService<ServiceBusHost>();
            }
        }

        /// <summary>
        /// Register a queue to be used by the application
        /// </summary>
        /// <param name="services"></param>
        /// <param name="queueName">The queue's name to register</param>
        /// <returns>The options object. You can use it to customize the registration.</returns>
        public static QueueOptions RegisterServiceBusQueue(this IServiceCollection services, string queueName)
        {
            RegisterBaseServices(services);

            var queue = new QueueOptions(services, queueName);
            services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.RegisterQueue(queue);
                });
            return queue;
        }

        /// <summary>
        /// Register a topic to be used by the application
        /// </summary>
        /// <param name="services"></param>
        /// <param name="topicName">The topic's name to register</param>
        /// <returns>The options object. You can use it to customize the registration.</returns>
        public static TopicOptions RegisterServiceBusTopic(this IServiceCollection services, string topicName)
        {
            RegisterBaseServices(services);

            var topic = new TopicOptions(topicName);
            services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.RegisterTopic(topic);
                });
            return topic;
        }

        /// <summary>
        /// Register a subscription to be used by the application.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="topicName">The topic's name related to the subscription.</param>
        /// <param name="subscriptionName">The subscription' name to register</param>
        /// <returns>The options object. You can use it to customize the registration.</returns>
        public static SubscriptionOptions RegisterServiceBusSubscription(this IServiceCollection services, string topicName, string subscriptionName)
        {
            RegisterBaseServices(services);

            var subscriptionOptions = new SubscriptionOptions(services, topicName, subscriptionName);
            services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.RegisterSubscription(subscriptionOptions);
                });
            return subscriptionOptions;
        }
    }
}
