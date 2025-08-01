using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.UnitTests.Helpers;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests.Core;

public class QueueClientHandlingTest
{
    [Fact]
    public async Task ClosesTheQueueClientsProperlyOnShutdown()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("testQueue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
            services.RegisterServiceBusDispatch().ToQueue("testQueue2", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
            services.RegisterServiceBusDispatch().ToQueue("testQueue3", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
        });

        var provider = await composer.Compose();

        var factory = composer.ClientFactory;
        var clientMocks = factory.GetAllSenderMocks();

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
    public async Task FailsSilentlyIfAQueueClientDoesNotCloseProperlyOnShutdown()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("testQueue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
            services.RegisterServiceBusDispatch().ToQueue("testQueue2", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
            services.RegisterServiceBusDispatch().ToQueue("testQueue3", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
        });

        var provider = await composer.Compose();

        var factory = composer.ClientFactory;
        var clientMocks = factory.GetAllSenderMocks();

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
    public async Task DontCallCloseWhenTheQueueClientIsAlreadyClosing()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("testQueue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
            services.RegisterServiceBusDispatch().ToQueue("testQueue2", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
            services.RegisterServiceBusDispatch().ToQueue("testQueue3", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
        });

        var provider = await composer.Compose();

        var factory = composer.ClientFactory;
        var clientMocks = factory.GetAllSenderMocks();

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
}