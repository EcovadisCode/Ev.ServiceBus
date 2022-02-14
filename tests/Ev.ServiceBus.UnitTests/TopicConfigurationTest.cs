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

public class TopicConfigurationTest
{
    [Fact]
    public async Task CannotRegisterTwoTopicsWithTheSameName()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusTopic("testTopic");
            services.RegisterServiceBusTopic("testTopic");
        });

        await Assert.ThrowsAnyAsync<DuplicateSenderRegistrationException>(async () => await composer.Compose());
    }

    [Fact]
    public async Task CanRegisterAndRetrieveTopics()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusTopic("testTopic").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            services.RegisterServiceBusTopic("testTopic2").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
            services.RegisterServiceBusTopic("testTopic3").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
        });

        var provider = await composer.Compose();

        var registry = provider.GetService<IServiceBusRegistry>();

        Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        Assert.Equal("testTopic2", registry.GetTopicSender("testTopic2")?.Name);
        Assert.Equal("testTopic3", registry.GetTopicSender("testTopic3")?.Name);
    }

    [Fact]
    public async Task CanRegisterTopicWithConnectionString()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusTopic("testTopic").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            });

        var provider = await composer.Compose();

        var registry = provider.GetService<IServiceBusRegistry>();

        Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
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
        services.RegisterServiceBusTopic("testTopic").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());

        var provider = services.BuildServiceProvider();
        await provider.SimulateStartHost(token: new CancellationToken());
        var composer = new Composer();
        composer.OverrideClientFactory(new FailingClientFactory());

        var registry = provider.GetService<ServiceBusRegistry>();
        await registry.GetTopicSender("testTopic").SendMessageAsync(new ServiceBusMessage());
    }

    [Fact]
    public async Task FailsSilentlyWhenRegisteringQueueWithNoConnectionAndNoDefaultConnection()
    {
        var composer = new Composer();
        composer.WithDefaultSettings(settings => { });

        var logger = new Mock<ILogger<SenderWrapper>>();
        composer.WithAdditionalServices(
            services =>
            {
                services.AddSingleton(logger.Object);
                services.RegisterServiceBusTopic("testTopic");
            });

        var provider = await composer.Compose();

        var registry = provider.GetService<IServiceBusRegistry>();
        await Assert.ThrowsAsync<MessageSenderUnavailableException>(
            async () =>
            {
                await registry.GetTopicSender("testTopic").SendMessageAsync(new ServiceBusMessage());
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
                services.RegisterServiceBusTopic("testTopic");
            });

        var provider = await composer.Compose();

        var registry = provider.GetService<IServiceBusRegistry>();

        composer.ClientFactory.GetAssociatedMock("testConnectionStringFromDefault").Senders.Count.Should().Be(1);
        Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
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
                services.RegisterServiceBusTopic("testTopic").WithConnection("Endpoint=concreteTestConnectionString;", new ServiceBusClientOptions());
            });

        var provider = await composer.Compose();

        var registry = provider.GetService<IServiceBusRegistry>();

        composer.ClientFactory.GetAssociatedMock("concreteTestConnectionString").Senders.Count.Should().Be(1);
        Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
    }
}