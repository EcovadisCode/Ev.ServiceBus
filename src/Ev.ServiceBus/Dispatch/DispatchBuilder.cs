using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.Dispatch
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

            var queue = new QueueOptions(_services, queueName, false);
            _services.Configure<ServiceBusOptions>(
                opts =>
                {
                    opts.RegisterQueue(queue);
                });
            var builder = new DispatchRegistrationBuilder(_services, queue);
            settings(builder);
        }

        public void ToTopic(string topicName, Action<DispatchRegistrationBuilder> settings)
        {
            if (topicName == null)
            {
                throw new ArgumentNullException(nameof(topicName));
            }

            var topic = new TopicOptions(topicName, false);
            _services.Configure<ServiceBusOptions>(
                options =>
                {
                    options.RegisterTopic(topic);
                });
            var builder = new DispatchRegistrationBuilder(_services, topic);
            settings(builder);
        }
    }
}
