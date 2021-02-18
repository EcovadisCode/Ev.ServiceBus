using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Dispatch;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception;
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
        public static IServiceCollection AddServiceBus<TMessagePayloadParser>(
            this IServiceCollection services,
            Action<ServiceBusSettings> config)
            where TMessagePayloadParser : class, IMessagePayloadParser
        {
            RegisterBaseServices(services);

            services.TryAddSingleton<IMessagePayloadParser, TMessagePayloadParser>();
            services.Configure<ServiceBusOptions>(
                options =>
                {
                    config(options.Settings);
                });

            return services;
        }

        private static void RegisterBaseServices(IServiceCollection services)
        {
            services.AddLogging();

            RegisterResourceManagementServices(services);

            RegisterMessageDispatchServices(services);

            RegisterMessageReceptionServices(services);

        }

        private static void RegisterMessageReceptionServices(IServiceCollection services)
        {
            services.TryAddSingleton<ReceptionRegistry>();
            services.TryAddScoped<MessageReceptionHandler>();
        }

        private static void RegisterMessageDispatchServices(IServiceCollection services)
        {
            services.TryAddSingleton<DispatchRegistry>();
            services.TryAddScoped<MessageDispatcher>();
            services.TryAddScoped<IMessagePublisher>(provider => provider.GetService<MessageDispatcher>());
            services.TryAddScoped<IMessageDispatcher>(provider => provider.GetService<MessageDispatcher>());
            services.TryAddSingleton<IDispatchSender, DispatchSender>();
        }

        private static void RegisterResourceManagementServices(IServiceCollection services)
        {
            services.TryAddSingleton<ServiceBusRegistry>();
            if (services.Any(o => o.ServiceType == typeof(IServiceBusRegistry)) == false)
            {
                services.AddSingleton<IServiceBusRegistry, ServiceBusRegistry>(provider =>
                    provider.GetRequiredService<ServiceBusRegistry>());
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

        public static TOptions ToMessageReceptionHandling<TOptions>(
            this TOptions options,
            int maxConcurrentCalls = 1,
            TimeSpan? maxAutoRenewDuration = null)
            where TOptions : ReceiverOptions
        {
            options.WithCustomMessageHandler<MessageReceptionHandler>(
                config =>
                {
                    config.AutoComplete = true;
                    config.MaxConcurrentCalls = maxConcurrentCalls;
                    if (maxAutoRenewDuration != null)
                    {
                        config.MaxAutoRenewDuration = maxAutoRenewDuration.Value;
                    }
                });
            return options;
        }

        public static ReceptionBuilder RegisterServiceBusReception(this IServiceCollection services)
        {
            RegisterBaseServices(services);
            return new(services);
        }

        public static DispatchBuilder RegisterServiceBusDispatch(this IServiceCollection services)
        {
            RegisterBaseServices(services);
            return new(services);
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

            var queue = new QueueOptions(services, queueName, true);
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

            var topic = new TopicOptions(topicName, true);
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

            var subscriptionOptions = new SubscriptionOptions(services, topicName, subscriptionName, true);
            services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.RegisterSubscription(subscriptionOptions);
                });
            return subscriptionOptions;
        }
    }
}
