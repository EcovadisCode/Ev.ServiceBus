using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.Reception
{
    public class ReceptionBuilder
    {
        private readonly IServiceCollection _services;

        public ReceptionBuilder(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// Configures the payload models to be received from the queue named <param name="queueName"></param>.
        /// </summary>
        /// <param name="queueName">The name of the queue that will receive the messages</param>
        /// <param name="settings">A callback to configure the payloads</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void FromQueue(string queueName, Action<ReceptionRegistrationBuilder> settings)
        {
            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            var queue = new QueueOptions(_services, queueName, false)
                .ToMessageReceptionHandling();
            _services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.RegisterQueue(queue);
                });
            var builder = new ReceptionRegistrationBuilder(_services, queue);
            settings(builder);
        }

        /// <summary>
        /// Configures the payload models to be received from the subscription named <param name="subscriptionName"></param>
        /// and coming from the topic <param name="topicName"></param>.
        /// </summary>
        /// <param name="topicName">The name of the related topic</param>
        /// <param name="subscriptionName">The name of the subscription that will receive the messages</param>
        /// <param name="settings">A callback to configure the payloads</param>
        /// <exception cref="ArgumentNullException"></exception>
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
                .ToMessageReceptionHandling();
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
