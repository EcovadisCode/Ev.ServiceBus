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

    public class ReceptionBuilder
    {
        private readonly IServiceCollection _services;

        public ReceptionBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public ReceptionRegistrationBuilder FromQueue(string queueName)
        {
            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            var options = _services.RegisterServiceBusQueue(queueName)
                .ToIntegrationEventHandling();
            return new ReceptionRegistrationBuilder(_services, options);
        }

        public ReceptionRegistrationBuilder FromSubscription(string topicName, string subscriptionName)
        {
            if (topicName == null)
            {
                throw new ArgumentNullException(nameof(topicName));
            }

            if (subscriptionName == null)
            {
                throw new ArgumentNullException(nameof(subscriptionName));
            }

            var options = _services.RegisterServiceBusSubscription(topicName, subscriptionName)
                .ToIntegrationEventHandling();
            return new ReceptionRegistrationBuilder(_services, options);
        }
    }

    public class ReceptionRegistrationBuilder
    {
        private readonly IServiceCollection _services;

        public ReceptionRegistrationBuilder(IServiceCollection services, ReceiverOptions receiverOptions)
        {
            _services = services;
            Options = receiverOptions;
        }

        public ReceiverOptions Options { get; }

        public MessageReceptionRegistration RegisterReception<TReceptionModel, THandler>()
            where THandler : class, IIntegrationEventHandler<TReceptionModel>
        {
            _services.TryAddScoped<THandler>();
            var builder = new MessageReceptionRegistration(Options, typeof(TReceptionModel), typeof(THandler));
            _services.AddSingleton(builder);
            return builder;
        }
    }
}
