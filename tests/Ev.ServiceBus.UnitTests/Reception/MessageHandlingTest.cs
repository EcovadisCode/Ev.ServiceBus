using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.UnitTests.Reception;

public class MessageHandlingTest
{
    [Fact]
    public async Task AScopeIsCreatedForEachMessageReceived()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.AddSingleton<InstanceRegistry>();
                services.AddSingleton<SingletonObject>();
                services.AddScoped<ScopedObject>();
                services.AddTransient<TransientObject>();

                services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
                {
                    builder.CustomizeConnection("Endpoint=connectionStringTest;", new ServiceBusClientOptions());
                    builder.RegisterReception<NoiseEvent, InstanceCounterMessageHandler>();
                });

            });

        var provider = await composer.Compose();

        var clientMock = composer.ClientFactory.GetProcessorMock("testQueue");

        await clientMock.TriggerMessageReception(TestMessageHelper.CreateEventMessage(nameof(NoiseEvent), new NoiseEvent()), new CancellationToken());
        await clientMock.TriggerMessageReception(TestMessageHelper.CreateEventMessage(nameof(NoiseEvent), new NoiseEvent()), new CancellationToken());

        var registry = provider.GetService<InstanceRegistry>();

        var numberOfSingletonInstances =
            registry.Instances.Where(o => o is SingletonObject).GroupBy(o => o).Count();
        var numberOfScopedInstances = registry.Instances.Where(o => o is ScopedObject).GroupBy(o => o).Count();
        var numberOfTransientInstances =
            registry.Instances.Where(o => o is TransientObject).GroupBy(o => o).Count();

        Assert.Equal(1, numberOfSingletonInstances);
        Assert.Equal(2, numberOfScopedInstances);
        Assert.Equal(4, numberOfTransientInstances);
    }

    private class TransientObject
    {
    }

    private class ScopedObject
    {
    }

    private class SingletonObject
    {
    }

    private class InstanceRegistry
    {
        public List<object> Instances { get; } = new List<object>();
    }

    private class InstanceCounterMessageHandler : IMessageReceptionHandler<NoiseEvent>
    {
        private readonly IServiceProvider _provider;

        public InstanceCounterMessageHandler(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task Handle(NoiseEvent @event, CancellationToken cancellationToken)
        {
            var registry = _provider.GetService<InstanceRegistry>();
            registry.Instances.Add(_provider.GetService<TransientObject>());
            registry.Instances.Add(_provider.GetService<TransientObject>());
            registry.Instances.Add(_provider.GetService<ScopedObject>());
            registry.Instances.Add(_provider.GetService<ScopedObject>());
            registry.Instances.Add(_provider.GetService<SingletonObject>());
            registry.Instances.Add(_provider.GetService<SingletonObject>());
            return Task.CompletedTask;
        }
    }
}