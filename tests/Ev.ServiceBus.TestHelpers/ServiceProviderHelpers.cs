using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.TestHelpers;
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

        public static QueueClientMock GetQueueClientMock(this IServiceProvider provider, string queueName, bool isReceiver = true)
        {
            var factory = provider.GetRequiredService<FakeQueueClientFactory>();
            return factory.GetAssociatedMock(queueName, isReceiver);
        }

        public static TopicClientMock GetTopicClientMock(this IServiceProvider provider, string topicName)
        {
            var factory = provider.GetRequiredService<FakeTopicClientFactory>();
            return factory.GetAllRegisteredTopicClients().FirstOrDefault(o => o.ClientName == topicName);
        }

        public static SubscriptionClientMock GetSubscriptionClientMock(this IServiceProvider provider, string subscriptionName)
        {
            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
            return factory.GetAllRegisteredClients().FirstOrDefault(o => o.ClientName == subscriptionName);
        }
    }
}
