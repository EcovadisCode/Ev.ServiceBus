using System;
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
}
