using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class ReceptionBuilder
    {
        private readonly IServiceCollection _services;

        public ReceptionBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void FromQueue(string queueName, Action<ReceptionRegistrationBuilder> settings)
        {
            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            var queue = new QueueOptions(_services, queueName, false)
                .ToIntegrationEventHandling();
            _services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.RegisterQueue(queue);
                });
            var builder = new ReceptionRegistrationBuilder(_services, queue);
            settings(builder);
        }

        public void FromSubscription(
            string topicName,
            string subscriptionName,
            Action<ReceptionRegistrationBuilder> settings)
        {
            if (topicName == null)
            {
                throw new ArgumentNullException(nameof(topicName));
            }

            if (subscriptionName == null)
            {
                throw new ArgumentNullException(nameof(subscriptionName));
            }

            var subscriptionOptions = new SubscriptionOptions(_services, topicName, subscriptionName, false)
                .ToIntegrationEventHandling();
            _services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.RegisterSubscription(subscriptionOptions);
                });
            var builder = new ReceptionRegistrationBuilder(_services, subscriptionOptions);
            settings(builder);
        }
    }
}
