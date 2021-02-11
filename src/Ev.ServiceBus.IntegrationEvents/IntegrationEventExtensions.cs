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
            TimeSpan maxAutoRenewDuration = default)
            where TOptions : ReceiverOptions
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

        public static ReceptionBuilder RegisterServiceBusReception(this IServiceCollection services)
        {
            return new(services);
        }

        public static DispatchBuilder RegisterServiceBusDispatch(this IServiceCollection services)
        {
            return new(services);
        }
    }
}
