using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.IntegrationEvents.Publication;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit.Sdk;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers
{
    public class Composer : IDisposable
    {
        private readonly List<Action<IServiceCollection>> _additionalServices;

        public Composer()
        {
            _additionalServices = new List<Action<IServiceCollection>>(5);
        }

        private readonly List<KeyValuePair<SenderType, string>> _listOfIntegrationEventSenders = new List<KeyValuePair<SenderType, string>>(5);

        public ServiceProvider Provider { get; private set; }
        public FakeSubscriptionClientFactory SubscriptionFactory { get; private set; }
        public FakeTopicClientFactory TopicFactory { get; private set; }
        public FakeQueueClientFactory QueueFactory { get; private set; }

        public void Dispose()
        {
            if (Provider != null)
            {
                SimulateStopHost(Provider, default(CancellationToken)).GetAwaiter().GetResult();
                Provider.Dispose();
                Provider = null;
            }
        }

        public Composer WithAdditionalServices(Action<IServiceCollection> action)
        {
            _additionalServices.Add(action);
            return this;
        }

        public Composer WithIntegrationEventsQueueSender(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new NullException(queueName);
            }

            _listOfIntegrationEventSenders.Add(new KeyValuePair<SenderType, string>(SenderType.Queue, queueName));

            return this;
        }

        private void ComposeSenders(IServiceCollection services)
        {
            if (_listOfIntegrationEventSenders.Count == 0)
            {
                return;
            }

            var serviceBusRegistry = new Mock<IServiceBusRegistry>();
            foreach (var integrationEventSender in _listOfIntegrationEventSenders)
            {
                var messageSender = new Mock<IMessageSender>();
                switch (integrationEventSender.Key)
                {
                    case SenderType.Queue:
                        serviceBusRegistry.Setup(s => s.GetQueueSender(integrationEventSender.Value))
                            .Returns(messageSender.Object);
                        break;
                    case SenderType.Topic:
                        serviceBusRegistry.Setup(s => s.GetTopicSender(integrationEventSender.Value))
                            .Returns(messageSender.Object);
                        break;
                }
            }
            services.AddSingleton(s => serviceBusRegistry.Object);
            services.AddSingleton<IEnumerable<IIntegrationEventSender>>(s =>
                new List<IIntegrationEventSender>(new[]
                    { new ServiceBusIntegrationEventSender(s.GetService<IServiceBusRegistry>(), s.GetService<IMessageBodyParser>())}));
        }


        public async Task Compose()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddServiceBus(
                settings =>
                {
                    settings.WithConnection("testconnectionstring");
                });
            services.AddIntegrationEventHandling<BodyParser>();

            ComposeSenders(services);

            OverrideClientFactories(services);

            _additionalServices.ForEach(a => a?.Invoke(services));

            Provider = services.BuildServiceProvider();
            await SimulateStartHost(Provider, new CancellationToken());

            QueueFactory = Provider.GetService<FakeQueueClientFactory>();
            TopicFactory = Provider.GetService<FakeTopicClientFactory>();
            SubscriptionFactory = Provider.GetService<FakeSubscriptionClientFactory>();
        }

        private async Task SimulateStartHost(IServiceProvider provider, CancellationToken token)
        {
            var hostedServices = provider.GetServices<IHostedService>();

            foreach (var hostedService in hostedServices)
            {
                await hostedService.StartAsync(token);
            }
        }

        private async Task SimulateStopHost(IServiceProvider provider, CancellationToken token)
        {
            var hostedServices = provider.GetServices<IHostedService>();

            foreach (var hostedService in hostedServices)
            {
                await hostedService.StopAsync(token);
            }
        }

        private void OverrideClientFactories(IServiceCollection services)
        {
            services.AddSingleton<FakeQueueClientFactory>();
            services.AddSingleton<FakeTopicClientFactory>();
            services.AddSingleton<FakeSubscriptionClientFactory>();
            services.Replace(
                new ServiceDescriptor(
                    typeof(IClientFactory<QueueOptions, IQueueClient>),
                    provider => provider.GetRequiredService<FakeQueueClientFactory>(),
                    ServiceLifetime.Singleton));
            services.Replace(
                new ServiceDescriptor(
                    typeof(IClientFactory<TopicOptions, ITopicClient>),
                    provider => provider.GetRequiredService<FakeTopicClientFactory>(),
                    ServiceLifetime.Singleton));
            services.Replace(
                new ServiceDescriptor(
                    typeof(IClientFactory<SubscriptionOptions, ISubscriptionClient>),
                    provider => provider.GetRequiredService<FakeSubscriptionClientFactory>(),
                    ServiceLifetime.Singleton));
        }

        enum SenderType
        {
            Queue,
            Topic
        }
    }
}
