using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class QueueConfigurationTest
{
    [Fact]
    public async Task CannotRegisterTwoQueuesWithTheSameName()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusQueue("testQueue");
                services.RegisterServiceBusQueue("testQueue");
            });

        await Assert.ThrowsAnyAsync<DuplicateSenderRegistrationException>(
            async () => await composer.Compose());
    }

    [Fact]
    public async Task CanRegisterAndRetrieveQueues()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusQueue("testQueue").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                services.RegisterServiceBusQueue("testQueue2").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
                services.RegisterServiceBusQueue("testQueue3").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
            });

        var provider = await composer.Compose();

        var registry = provider.GetService<IServiceBusRegistry>();

        Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        Assert.Equal("testQueue2", registry.GetQueueSender("testQueue2")?.Name);
        Assert.Equal("testQueue3", registry.GetQueueSender("testQueue3")?.Name);
    }

    [Fact]
    public async Task CanRegisterQueueWithConnectionString()
    {
        var composer = new Composer();
        var serviceBusClientOptions = new ServiceBusClientOptions();

        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusQueue("testQueue").WithConnection("Endpoint=testConnectionString;", serviceBusClientOptions);
            });

        var provider = await composer.Compose();

        var registry = provider.GetRequiredService<IServiceBusRegistry>();

        composer.ClientFactory.GetSenderMock("testQueue").Should().NotBeNull();
        composer.ClientFactory.GetProcessorMock("testQueue").Should().BeNull();
        composer.ClientFactory.GetSessionProcessorMock("testQueue").Should().BeNull();

        Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
    }

    [Fact]
    public async Task FailsSilentlyWhenAnErrorOccursBuildingAQueueClient()
    {
        var composer = new Composer();
        composer.OverrideClientFactory(new FailingClientFactory());
        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusQueue("testQueue").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            });

        var provider = await composer.Compose();

        var registry = provider.GetService<ServiceBusRegistry>();

        await Assert.ThrowsAsync<MessageSenderUnavailableException>(
            async () =>
            {
                await registry.GetQueueSender("testQueue").SendMessageAsync(new ServiceBusMessage());
            });
    }

    [Fact]
    public async Task DoesntThrowExceptionWhenServiceBusIsDeactivated()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddServiceBus<PayloadSerializer>(
            settings =>
            {
                settings.Enabled = false;
            });
        services.RegisterServiceBusQueue("testQueue").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        services.OverrideClientFactory(new FailingClientFactory());

        var provider = services.BuildServiceProvider();
        await provider.SimulateStartHost(new CancellationToken());

        var registry = provider.GetService<ServiceBusRegistry>();
        await registry.GetQueueSender("testQueue").SendMessageAsync(new ServiceBusMessage());
    }

    [Fact]
    public async Task FailsSilentlyWhenRegisteringQueueWithNoConnectionAndNoDefaultConnection()
    {
        var composer = new Composer();

        var logger = new Mock<ILogger<ServiceBusEngine>>();
        composer.WithDefaultSettings(settings => {});
        composer.WithAdditionalServices(
            services =>
            {
                services.AddSingleton(logger.Object);
                services.RegisterServiceBusQueue("testQueue");
            });

        var provider = await composer.Compose();

        var registry = provider.GetService<IServiceBusRegistry>();
        await Assert.ThrowsAsync<MessageSenderUnavailableException>(
            async () =>
            {
                await registry.GetQueueSender("testQueue").SendMessageAsync(new ServiceBusMessage());
            });
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<MissingConnectionException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UsesDefaultConnectionWhenNoConnectionIsProvided()
    {
        var composer = new Composer();

        composer.WithDefaultSettings(
            settings =>
            {
                settings.WithConnection("Endpoint=testConnectionStringFromDefault;", new ServiceBusClientOptions());
            });
        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusQueue("testQueue");
            });

        var provider = await composer.Compose();

        composer.ClientFactory.GetAssociatedMock("testConnectionStringFromDefault").Should().NotBeNull();
        var registry = provider.GetService<IServiceBusRegistry>();

        Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
    }

    [Fact]
    public async Task OverridesDefaultConnectionWhenConcreteConnectionIsProvided()
    {
        var composer = new Composer();
        composer.WithDefaultSettings(
            settings =>
            {
                settings.WithConnection("Endpoint=testConnectionStringFromDefault;", new ServiceBusClientOptions());
            });
        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusQueue("testQueue").WithConnection("Endpoint=concreteTestConnectionString;", new ServiceBusClientOptions());
            });

        var provider = await composer.Compose();

        var connection = composer.ClientFactory.GetAssociatedMock("concreteTestConnectionString");
        connection.GetSenderMock("testQueue").Should().NotBeNull();
        var registry = provider.GetService<IServiceBusRegistry>();

        Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
    }
}