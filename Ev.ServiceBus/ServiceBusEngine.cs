using System;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus
{
    public class ServiceBusEngine
    {
        private readonly ILogger<ServiceBusEngine> _logger;
        private readonly IOptions<ServiceBusOptions> _options;
        private readonly ServiceBusRegistry _registry;
        private readonly IServiceProvider _provider;

        public ServiceBusEngine(
            ILogger<ServiceBusEngine> logger,
            IOptions<ServiceBusOptions> options,
            ServiceBusRegistry registry,
            IServiceProvider provider)
        {
            _logger = logger;
            _options = options;
            _registry = registry;
            _provider = provider;
        }

        public void StartAll()
        {
            _logger.LogInformation("Starting azure service bus clients");

            foreach (var queueOptions in _options.Value.Queues)
            {
                BuildAndRegisterQueue(queueOptions, _options.Value);
            }

            foreach (var topicOptions in _options.Value.Topics)
            {
                BuildAndRegisterTopic(topicOptions, _options.Value);
            }

            foreach (var subscriptionOptions in _options.Value.Subscriptions)
            {
                BuildAndRegisterSubscription(subscriptionOptions, _options.Value);
            }
        }

        private void BuildAndRegisterQueue(QueueOptions options, ServiceBusOptions parentOptions)
        {
            var queue = new QueueWrapper(options, parentOptions, _provider);
            queue.Initialize();

            _registry.Register(queue);
        }

        private void BuildAndRegisterTopic(TopicOptions options, ServiceBusOptions parentOptions)
        {
            var topic = new TopicWrapper(options, parentOptions, _provider);
            topic.Initialize();

            _registry.Register(topic);
        }

        private void BuildAndRegisterSubscription(SubscriptionOptions options, ServiceBusOptions parentOptions)
        {
            var subscription = new SubscriptionWrapper(options, parentOptions, _provider);
            subscription.Initialize();

            _registry.Register(subscription);
        }

        public async Task StopAll()
        {
            _logger.LogInformation("Stopping azure service bus clients");

            await Task.WhenAll(_registry.GetAllQueues().Select(CloseQueueAsync).ToArray()).ConfigureAwait(false);
            await Task.WhenAll(_registry.GetAllTopics().Select(CloseTopicAsync).ToArray()).ConfigureAwait(false);
            await Task.WhenAll(_registry.GetAllSubscriptions().Select(CloseSubscriptionAsync).ToArray()).ConfigureAwait(false);
        }

        private async Task CloseQueueAsync(QueueWrapper queue)
        {
            if (queue.QueueClient.IsClosedOrClosing)
            {
                return;
            }

            try
            {
                await queue.QueueClient.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Closing of QueueClient {queue.Name} failed");
            }
        }

        private async Task CloseTopicAsync(TopicWrapper topic)
        {
            if (topic.TopicClient.IsClosedOrClosing)
            {
                return;
            }

            try
            {
                await topic.TopicClient.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Closing of topic Client {topic.Name} failed");
            }
        }

        private async Task CloseSubscriptionAsync(SubscriptionWrapper subscription)
        {
            if (subscription.SubscriptionClient.IsClosedOrClosing)
            {
                return;
            }

            try
            {
                await subscription.SubscriptionClient.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Closing of subscription Client {subscription.Name} failed");
            }
        }
    }
}