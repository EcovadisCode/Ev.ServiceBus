using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
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
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options.RegisterTopic("testtopic1").WithConnectionString("testConnectionString1");
                    options.RegisterTopic("testtopic2").WithConnectionString("testConnectionString2");
                    options.RegisterTopic("testtopic3").WithConnectionString("testConnectionString3");
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

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
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options.RegisterTopic("testtopic1").WithConnectionString("testConnectionString1");
                    options.RegisterTopic("testtopic2").WithConnectionString("testConnectionString2");
                    options.RegisterTopic("testtopic3").WithConnectionString("testConnectionString3");
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

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
    }
}
