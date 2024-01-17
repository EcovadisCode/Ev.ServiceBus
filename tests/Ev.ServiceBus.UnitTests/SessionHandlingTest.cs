using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class SessionHandlingTest
{
    [Fact]
    public async Task CanReceiveSessionMessageFromQueue()
    {
        var eventStore = new EventStore();
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusReception()
                    .FromQueue("testQueue", builder =>
                    {
                        builder.EnableSessionHandling(_ => {});
                        builder.RegisterReception<SubscribedEvent, ReceptionTest.SubscribedPayloadHandler>();
                    });

                services.AddSingleton(eventStore);
            });

        await composer.Compose();

        var client = composer.ClientFactory.GetSessionProcessorMock("testQueue");

        var message = TestMessageHelper.CreateEventMessage("SubscribedEvent", new
        {
            SomeString = "hello",
            SomeNumber = 36
        });
        await client.TriggerMessageReception(message, CancellationToken.None);

        eventStore.Events.Count.Should().Be(1);
    }

    [Fact]
    public async Task CanReceiveSessionMessageFromSubscription()
    {
        var eventStore = new EventStore();
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusReception()
                    .FromSubscription("testTopic", "testSubscription", builder =>
                    {
                        builder.EnableSessionHandling(_ => {});
                        builder.RegisterReception<SubscribedEvent, ReceptionTest.SubscribedPayloadHandler>();
                    });

                services.AddSingleton(eventStore);
            });

        await composer.Compose();

        var client = composer.ClientFactory.GetSessionProcessorMock("testTopic", "testSubscription");

        var message = TestMessageHelper.CreateEventMessage("SubscribedEvent", new
        {
            SomeString = "hello",
            SomeNumber = 36
        });
        await client.TriggerMessageReception(message, CancellationToken.None);

        eventStore.Events.Count.Should().Be(1);
    }

    [Fact]
    public async Task DefinedOptionsAreSetProperly()
    {
        var services = new ServiceCollection();
        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });

        services.RegisterServiceBusReception()
            .FromSubscription("testTopic", "testSubscription", builder =>
            {
                builder.EnableSessionHandling(options =>
                {
                    options.MaxConcurrentSessions = 3;
                    options.MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(13);
                });
                builder.RegisterReception<SubscribedEvent, ReceptionTest.SubscribedPayloadHandler>();
            });

        services.OverrideClientFactory();
        var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();

        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var client = provider.GetSessionProcessorMock("testTopic", "testSubscription");

        client.Options.Should().NotBeNull();
        client.Options.MaxConcurrentSessions.Should().Be(3);
        client.Options.MaxAutoLockRenewalDuration.Should().Be(TimeSpan.FromSeconds(13));
    }
}
