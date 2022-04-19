using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Dispatch;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: InternalsVisibleTo("Ev.ServiceBus.UnitTests")]
[assembly: InternalsVisibleTo("Ev.ServiceBus.TestHelpers")]

namespace Ev.ServiceBus
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers basic services for using ServiceBus.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config">Lambda expression to configure Service Bus</param>
        /// <typeparam name="TMessagePayloadSerializer">Type of the serializer to use.</typeparam>
        /// <returns></returns>
        public static ServiceBusBuilder AddServiceBus<TMessagePayloadSerializer>(
            this IServiceCollection services,
            Action<ServiceBusSettings> config)
            where TMessagePayloadSerializer : class, IMessagePayloadSerializer
        {
            RegisterBaseServices(services);

            services.TryAddSingleton<IMessagePayloadSerializer, TMessagePayloadSerializer>();
            services.Configure<ServiceBusOptions>(
                options =>
                {
                    config(options.Settings);
                });

            return new ServiceBusBuilder(services);
        }

        private static void RegisterBaseServices(IServiceCollection services)
        {
            services.AddLogging();

            RegisterResourceManagementServices(services);

            RegisterMessageDispatchServices(services);

            RegisterMessageReceptionServices(services);

            services.TryAddScoped<IMessageMetadataAccessor, MessageMetadataAccessor>();
        }

        private static void RegisterMessageReceptionServices(IServiceCollection services)
        {
            services.TryAddScoped<MessageReceptionHandler>();
        }

        private static void RegisterMessageDispatchServices(IServiceCollection services)
        {
            services.TryAddScoped<MessageDispatcher>();
            services.TryAddScoped<IMessagePublisher>(provider => provider.GetRequiredService<MessageDispatcher>());
            services.TryAddScoped<IMessageDispatcher>(provider => provider.GetRequiredService<MessageDispatcher>());
            services.TryAddScoped<IDispatchSender, DispatchSender>();
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
            services.TryAddSingleton<IClientFactory, ClientFactory>();

            if (services.Any(o => o.ImplementationType == typeof(ServiceBusHost)) == false)
            {
                services.AddHostedService<ServiceBusHost>();
            }
        }

        /// <summary>
        /// Define that messages received by the receiver will go to the <see cref="MessageReceptionHandler"/> handler.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="config"></param>
        /// <typeparam name="TOptions"></typeparam>
        /// <returns></returns>
        public static TOptions ToMessageReceptionHandling<TOptions>(
            this TOptions options,
            Action<ServiceBusProcessorOptions> config)
            where TOptions : ReceiverOptions
        {
            options.WithCustomMessageHandler<MessageReceptionHandler>(config);
            return options;
        }

        /// <summary>
        /// The start of the registration process for message reception
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ReceptionBuilder RegisterServiceBusReception(this IServiceCollection services)
        {
            RegisterBaseServices(services);
            return new(services);
        }

        /// <summary>
        /// The start of the registration process for message dispatch
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
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
