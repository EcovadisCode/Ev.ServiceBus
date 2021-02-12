using System;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class DispatchBuilder
    {
        private readonly IServiceCollection _services;

        public DispatchBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void ToQueue(string queueName, Action<DispatchRegistrationBuilder> settings)
        {
            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            var options = _services.RegisterServiceBusQueue(queueName);
            var builder = new DispatchRegistrationBuilder(_services, options);
            settings(builder);
        }

        public void ToTopic(string topicName, Action<DispatchRegistrationBuilder> settings)
        {
            if (topicName == null)
            {
                throw new ArgumentNullException(nameof(topicName));
            }

            var options = _services.RegisterServiceBusTopic(topicName);
            var builder = new DispatchRegistrationBuilder(_services, options);
            settings(builder);
        }
    }
}
