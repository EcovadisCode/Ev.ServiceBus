using System;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.IntegrationEvents.Publication;
using Ev.ServiceBus.IntegrationEvents.Subscription;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.IntegrationEvents
{
    public static class IntegrationEventExtensions
    {
        public static IServiceCollection AddIntegrationEventHandling<TMessageBodyParser>(this IServiceCollection services)
            where TMessageBodyParser : class, IMessageBodyParser
        {
            services.AddLogging();
            services.TryAddSingleton<PublicationRegistry>();
            services.TryAddSingleton<ServiceBusEventSubscriptionRegistry>();

            services.TryAddScoped<IntegrationEventDispatcher>();
            services.TryAddScoped<IIntegrationEventPublisher>(
                provider => provider.GetService<IntegrationEventDispatcher>());
            services.TryAddScoped<IIntegrationEventDispatcher>(
                provider => provider.GetService<IntegrationEventDispatcher>());

            services.TryAddSingleton<IIntegrationEventSender, ServiceBusIntegrationEventSender>();
            services.TryAddSingleton<IMessageBodyParser, TMessageBodyParser>();
            return services;
        }

        public static SubscriptionOptions ToIntegrationEventHandling(
            this SubscriptionOptions options,
            int maxConcurrentCalls = 1,
            TimeSpan maxAutoRenewDuration = default)
        {
            options.WithCustomMessageHandler<IntegrationEventMessageHandler>(
                config =>
                {
                    config.AutoComplete = true;
                    config.MaxConcurrentCalls = maxConcurrentCalls;
                    if (maxAutoRenewDuration != default)
                    {
                        config.MaxAutoRenewDuration = maxAutoRenewDuration;
                    }
                });
            return options;
        }

        public static IServiceCollection RegisterIntegrationEventPublication<TIntegrationEvent>(
            this IServiceCollection services,
            Action<EventPublicationBuilder<TIntegrationEvent>> configure)
        {
            var options = new EventPublicationBuilder<TIntegrationEvent>();
            configure(options);
            options.Build(services);
            return services;
        }

        public static IServiceCollection RegisterIntegrationEventSubscription<TIntegrationEvent, THandler>(
            this IServiceCollection services,
            Action<EventSubscriptionBuilder<TIntegrationEvent, THandler>> configure)
            where THandler : class, IIntegrationEventHandler<TIntegrationEvent>
        {
            var options = new EventSubscriptionBuilder<TIntegrationEvent, THandler>();
            configure(options);
            options.Build(services);
            return services;
        }
    }
}
