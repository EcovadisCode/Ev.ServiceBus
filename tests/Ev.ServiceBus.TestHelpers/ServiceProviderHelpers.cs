using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ev.ServiceBus.UnitTests.Helpers;

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

    public static SenderMock GetSenderMock(this IServiceProvider provider, string resourceId)
    {
        var factory = provider.GetRequiredService<FakeClientFactory>();
        return factory.GetSenderMock(resourceId);
    }

    public static ProcessorMock GetProcessorMock(this IServiceProvider provider, string queueName)
    {
        var factory = provider.GetRequiredService<FakeClientFactory>();
        return factory.GetProcessorMock(queueName);
    }

    public static ProcessorMock GetProcessorMock(this IServiceProvider provider, string topicName, string subscriptionName)
    {
        var factory = provider.GetRequiredService<FakeClientFactory>();
        return factory.GetProcessorMock(topicName, subscriptionName);
    }

    public static SessionProcessorMock GetSessionProcessorMock(this IServiceProvider provider, string queueName)
    {
        var factory = provider.GetRequiredService<FakeClientFactory>();
        return factory.GetSessionProcessorMock(queueName);
    }

    public static SessionProcessorMock GetSessionProcessorMock(this IServiceProvider provider, string topicName, string subscriptionName)
    {
        var factory = provider.GetRequiredService<FakeClientFactory>();
        return factory.GetSessionProcessorMock(topicName, subscriptionName);
    }

}