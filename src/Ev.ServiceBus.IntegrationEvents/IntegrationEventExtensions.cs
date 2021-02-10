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
        public static IServiceCollection AddIntegrationEventHandling<TMessageBodyParser>(
            this IServiceCollection services)
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

        public static TOptions ToIntegrationEventHandling<TOptions>(
            this TOptions options,
            int maxConcurrentCalls = 1,
            TimeSpan? maxAutoRenewDuration = null)
            where TOptions : ReceiverOptions
        {
            options.WithCustomMessageHandler<IntegrationEventMessageHandler>(
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

        public static IServiceCollection RegisterIntegrationEventPublication<TIntegrationEvent>(
            this IServiceCollection services,
            Action<EventPublicationBuilder<TIntegrationEvent>> configure)
        {
            var options = new EventPublicationBuilder<TIntegrationEvent>();
            configure(options);
            options.Build(services);
            return services;
        }

        public static ReceptionBuilder RegisterServiceBusReception(this IServiceCollection services)
        {
            return new(services);
        }
    }
}
