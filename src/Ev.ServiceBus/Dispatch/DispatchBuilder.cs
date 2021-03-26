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

        /// <summary>
        /// Configures the payload models to be dispatched to the queue named <param name="queueName"></param>.
        /// </summary>
        /// <param name="queueName">The name of the queue that will dispatch the messages</param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
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

        /// <summary>
        /// Configures the payload models to be dispatched to the topic named <param name="topicName"></param>.
        /// </summary>
        /// <param name="topicName">The name of the topic that will dispatch the messages</param>
        /// <param name="settings">A callback to configure the payloads</param>
        /// <exception cref="ArgumentNullException"></exception>
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
