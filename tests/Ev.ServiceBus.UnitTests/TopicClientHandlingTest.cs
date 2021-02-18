using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class TopicClientHandlingTest
    {
        [Fact]
        public async Task ClosesTheTopicClientsProperlyOnShutdown()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("testtopic1").WithConnection("testConnectionString1");
                services.RegisterServiceBusTopic("testtopic2").WithConnection("testConnectionString2");
                services.RegisterServiceBusTopic("testtopic3").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeTopicClientFactory>();
            var clientMocks = factory.GetAllRegisteredTopicClients();

            foreach (var clientMock in clientMocks)
            {
                clientMock.Mock.Setup(o => o.CloseAsync()).Returns(Task.CompletedTask).Verifiable();
            }

            await provider.SimulateStopHost(token: new CancellationToken());

            foreach (var clientMock in clientMocks)
            {
                clientMock.Mock.Verify(o => o.CloseAsync(), Times.Once);
            }
        }

        [Fact]
        public async Task FailsSilentlyIfATopicClientDoesNotCloseProperlyOnShutdown()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("testtopic1").WithConnection("testConnectionString1");
                services.RegisterServiceBusTopic("testtopic2").WithConnection("testConnectionString2");
                services.RegisterServiceBusTopic("testtopic3").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeTopicClientFactory>();
            var clientMocks = factory.GetAllRegisteredTopicClients();

            clientMocks[0].Mock.Setup(o => o.CloseAsync()).Returns(Task.CompletedTask).Verifiable();
            clientMocks[1].Mock.Setup(o => o.CloseAsync()).Throws<SocketException>().Verifiable();
            clientMocks[2].Mock.Setup(o => o.CloseAsync()).Returns(Task.CompletedTask).Verifiable();

            await provider.SimulateStopHost(token: new CancellationToken());

            foreach (var clientMock in clientMocks)
            {
                clientMock.Mock.Verify(o => o.CloseAsync(), Times.Once);
            }
        }

        [Fact]
        public async Task DontCallCloseWhenTheTopicClientIsAlreadyClosing()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("testtopic1").WithConnection("testConnectionString1");
                services.RegisterServiceBusTopic("testtopic2").WithConnection("testConnectionString2");
                services.RegisterServiceBusTopic("testtopic3").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeTopicClientFactory>();
            var clientMocks = factory.GetAllRegisteredTopicClients();

            foreach (var clientMock in clientMocks)
            {
                clientMock.Mock.SetupGet(o => o.IsClosedOrClosing).Returns(true);
                clientMock.Mock.Setup(o => o.CloseAsync()).Returns(Task.CompletedTask).Verifiable();
            }

            await provider.SimulateStopHost(token: new CancellationToken());

            foreach (var clientMock in clientMocks)
            {
                clientMock.Mock.Verify(o => o.CloseAsync(), Times.Never);
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
}
