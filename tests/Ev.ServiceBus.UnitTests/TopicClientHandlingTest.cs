using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class TopicClientHandlingTest
{
    [Fact]
    public async Task ClosesTheTopicClientsProperlyOnShutdown()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusTopic("testtopic1").WithConnection("Endpoint=testConnectionString1;", new ServiceBusClientOptions());
            services.RegisterServiceBusTopic("testtopic2").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
            services.RegisterServiceBusTopic("testtopic3").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
        });

        var provider = await composer.Compose();

        var clientMocks = composer.ClientFactory.GetAllSenderMocks();
        foreach (var clientMock in clientMocks)
        {
            clientMock.Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        }

        await provider.SimulateStopHost(token: new CancellationToken());

        foreach (var clientMock in clientMocks)
        {
            clientMock.Mock.Verify(o => o.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task FailsSilentlyIfATopicClientDoesNotCloseProperlyOnShutdown()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusTopic("testtopic1").WithConnection("Endpoint=testConnectionString1;", new ServiceBusClientOptions());
            services.RegisterServiceBusTopic("testtopic2").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
            services.RegisterServiceBusTopic("testtopic3").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
        });

        var provider = await composer.Compose();

        var clientMocks = composer.ClientFactory.GetAllSenderMocks();

        clientMocks[0].Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        clientMocks[1].Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Throws<SocketException>().Verifiable();
        clientMocks[2].Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        await provider.SimulateStopHost(token: new CancellationToken());

        foreach (var clientMock in clientMocks)
        {
            clientMock.Mock.Verify(o => o.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task DontCallCloseWhenTheTopicClientIsAlreadyClosing()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusTopic("testtopic1").WithConnection("Endpoint=testConnectionString1;", new ServiceBusClientOptions());
            services.RegisterServiceBusTopic("testtopic2").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
            services.RegisterServiceBusTopic("testtopic3").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
        });

        var provider = await composer.Compose();

        var clientMocks = composer.ClientFactory.GetAllSenderMocks();

        foreach (var clientMock in clientMocks)
        {
            clientMock.Mock.SetupGet(o => o.IsClosed).Returns(true);
            clientMock.Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        }

        await provider.SimulateStopHost(token: new CancellationToken());

        foreach (var clientMock in clientMocks)
        {
            clientMock.Mock.Verify(o => o.CloseAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task ThrowsExceptionWhenAQueueSenderIsNotFound()
    {
        var composer = new Composer();

        var provider = await composer.Compose();

        var registry = provider.GetService<IServiceBusRegistry>();

        var ex = Assert.Throws<TopicSenderNotFoundException>(
            () =>
            {
                registry.GetTopicSender("notARegisteredTopicName");
            });
        ex.TopicName.Should().Be("notARegisteredTopicName");
    }

}