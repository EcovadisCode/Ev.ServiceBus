using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class Composer : IDisposable
    {
        private readonly List<Action<IServiceCollection>> _additionalServices;
        private Action<IServiceCollection> _overrideFactory;
        private Action<ServiceBusSettings> _defaultSettings;

        public Composer()
        {
            _additionalServices = new List<Action<IServiceCollection>>(5);
            _overrideFactory = s => s.OverrideClientFactories();
            _defaultSettings = settings => { settings.WithConnection("testConnectionString"); };
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

        public void OverrideClientFactory<TOptions, TClient>(IClientFactory<TOptions, TClient> factory)
            where TOptions : ClientOptions where TClient : IClientEntity
        {
            _overrideFactory = s => s.OverrideClientFactory(factory);
        }

        public Composer WithAdditionalServices(Action<IServiceCollection> action)
        {
            _additionalServices.Add(action);
            return this;
        }

        public void WithIntegrationEventsQueueSender(string queueName)
        {
            _listOfIntegrationEventSenders.Add(new KeyValuePair<SenderType, string>(SenderType.Queue, queueName));
        }

        public void WithDefaultSettings(Action<ServiceBusSettings> defaultSettings)
        {
            _defaultSettings = defaultSettings;
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
        }


        public async Task<IServiceProvider> Compose()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(_defaultSettings);

            ComposeSenders(services);

            OverrideClientFactories(services);

            _overrideFactory(services);
            _additionalServices.ForEach(a => a?.Invoke(services));

            Provider = services.BuildServiceProvider();
            await SimulateStartHost(Provider, new CancellationToken());

            QueueFactory = Provider.GetService<FakeQueueClientFactory>();
            TopicFactory = Provider.GetService<FakeTopicClientFactory>();
            SubscriptionFactory = Provider.GetService<FakeSubscriptionClientFactory>();
            return Provider;
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
