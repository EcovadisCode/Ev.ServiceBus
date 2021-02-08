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

        public void FromQueue(string queueName, Action<ReceptionRegistrationBuilder> settings)
        {
            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            var options = _services.RegisterServiceBusQueue(queueName)
                .ToIntegrationEventHandling();
            var builder = new ReceptionRegistrationBuilder(_services, options);
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

            var options = _services.RegisterServiceBusSubscription(topicName, subscriptionName)
                .ToIntegrationEventHandling();
            var builder = new ReceptionRegistrationBuilder(_services, options);
            settings(builder);
        }
    }
}
