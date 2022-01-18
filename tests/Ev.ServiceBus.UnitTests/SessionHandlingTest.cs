using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
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
                        builder.EnableSessionHandling();
                        builder.RegisterReception<SubscribedEvent, ReceptionTest.SubscribedPayloadHandler>();
                    });

                services.AddSingleton(eventStore);
            });

        await composer.Compose();

        var clients = composer
            .QueueFactory
            .GetAllRegisteredClients();
        var client = clients.First(o => o.ClientName == "testQueue" && o.IsReceiver);

        var message = TestMessageHelper.CreateEventMessage("SubscribedEvent", new
        {
            SomeString = "hello",
            SomeNumber = 36
        });
        await client.TriggerSessionMessageReception(message, CancellationToken.None);

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
                        builder.EnableSessionHandling();
                        builder.RegisterReception<SubscribedEvent, ReceptionTest.SubscribedPayloadHandler>();
                    });

                services.AddSingleton(eventStore);
            });

        await composer.Compose();

        var clients = composer
            .SubscriptionFactory
            .GetAllRegisteredClients();
        var client = clients.First(o => o.ClientName == "testSubscription");

        var message = TestMessageHelper.CreateEventMessage("SubscribedEvent", new
        {
            SomeString = "hello",
            SomeNumber = 36
        });
        await client.TriggerSessionMessageReception(message, CancellationToken.None);

        eventStore.Events.Count.Should().Be(1);
    }

    [Fact]
    public async Task DefinedOptionsAreSetProperly()
    {
        var services = new ServiceCollection();
        services.AddServiceBus<PayloadSerializer>(settings =>
        {
            settings.WithConnection("testConnectionString");
        });

        services.RegisterServiceBusReception()
            .FromSubscription("testTopic", "testSubscription", builder =>
            {
                builder.EnableSessionHandling(3, TimeSpan.FromSeconds(13));
                builder.RegisterReception<SubscribedEvent, ReceptionTest.SubscribedPayloadHandler>();
            });

        var factory = new Factory();
        SessionHandlerOptions optionsToCheck = null;
        factory.Mock
            .Setup(o => o.RegisterSessionHandler(It.IsAny<Func<IMessageSession, Message, CancellationToken, Task>>(), It.IsAny<SessionHandlerOptions>()))
            .Callback((Func<IMessageSession, Message, CancellationToken, Task> messageHandler, SessionHandlerOptions options) =>
            {
                optionsToCheck = options;
            });

        services.Replace(
            new ServiceDescriptor(
                typeof(IClientFactory<SubscriptionOptions, ISubscriptionClient>),
                provider => factory,
                ServiceLifetime.Singleton));

        var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();

        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        optionsToCheck.Should().NotBeNull();
        optionsToCheck.MaxConcurrentSessions.Should().Be(3);
        optionsToCheck.MaxAutoRenewDuration.Should().Be(TimeSpan.FromSeconds(13));
    }

    private class Factory : IClientFactory<SubscriptionOptions, ISubscriptionClient>
    {
        public Factory()
        {
            Mock = new Mock<ISubscriptionClient>();
        }

        public Mock<ISubscriptionClient> Mock { get; }

        public ISubscriptionClient Create(SubscriptionOptions options, ConnectionSettings connectionSettings)
        {
            return Mock.Object;
        }
    }
}
