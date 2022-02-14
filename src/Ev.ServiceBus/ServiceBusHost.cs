using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ev.ServiceBus
{
    public class ServiceBusHost : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceBusHost(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var engine = _serviceProvider.GetRequiredService<ServiceBusEngine>();

            await engine.StartAll();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var engine = _serviceProvider.GetRequiredService<ServiceBusEngine>();

            await engine.StopAll(cancellationToken).ConfigureAwait(false);
        }
    }
}