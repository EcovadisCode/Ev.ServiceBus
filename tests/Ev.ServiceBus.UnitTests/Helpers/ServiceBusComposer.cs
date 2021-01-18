using System;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class ServiceBusComposer
    {
        private Action<IServiceCollection> _additionalServices;
        private Action<IServiceCollection> _overrideFactory;
        private Action<ServiceBusSettings> _defaultSettings;

        public ServiceBusComposer()
        {
            _additionalServices = _ => { };
            _overrideFactory = s => s.OverrideClientFactories();
            _defaultSettings = _ => { };
        }

        public ServiceBusComposer OverrideQueueClientFactory(IClientFactory factory)
        {
            _overrideFactory = s => s.OverrideQueueClientFactory(factory);
            return this;
        }

        public ServiceBusComposer WithAdditionalServices(Action<IServiceCollection> action)
        {
            _additionalServices = action;
            return this;
        }

        public ServiceBusComposer WithDefaultSettings(Action<ServiceBusSettings> defaultSettings)
        {
            _defaultSettings = defaultSettings;
            return this;
        }

        public async Task<IServiceProvider> ComposeAndSimulateStartup()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddServiceBus(_defaultSettings);

            _overrideFactory(services);
            _additionalServices(services);

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());
            return provider;
        }
    }
}
