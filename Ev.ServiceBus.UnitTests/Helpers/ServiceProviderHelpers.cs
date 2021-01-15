using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public static class ServiceProviderHelpers
    {
        public static async Task<IServiceProvider> SimulateStartHost(this IServiceProvider provider, CancellationToken token)
        {
            var hostedServices = provider.GetServices<IHostedService>();

            var serviceBusHost = hostedServices.OfType<ServiceBusHost>().Single();

            await serviceBusHost.StartAsync(token);

            return provider;
        }

        public static async Task<IServiceProvider> SimulateStopHost(this IServiceProvider provider, CancellationToken token)
        {
            var hostedServices = provider.GetServices<IHostedService>();

            var serviceBusHost = hostedServices.OfType<ServiceBusHost>().Single();

            await serviceBusHost.StopAsync(token);

            return provider;
        }

        public static QueueClientMock GetQueueClientMock(this IServiceProvider provider, string queueName)
        {
            var factory = provider.GetRequiredService<FakeClientFactory>();
            return factory.GetAssociatedMock(queueName);
        }

        public static SubscriptionClientMock GetSubscriptionClientMock(this IServiceProvider provider, string subscriptionName)
        {
            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
            return factory.GetAllRegisteredSubscriptionClients().FirstOrDefault(o => o.ClientName == subscriptionName);
        }
    }
}
