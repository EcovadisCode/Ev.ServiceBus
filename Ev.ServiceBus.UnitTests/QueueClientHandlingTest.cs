using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class QueueClientHandlingTest
    {
        [Fact]
        public async Task ClosesTheQueueClientsProperlyOnShutdown()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options.RegisterQueue("testQueue").WithConnectionString("testConnectionString");
                    options.RegisterQueue("testQueue2").WithConnectionString("testConnectionString2");
                    options.RegisterQueue("testQueue3").WithConnectionString("testConnectionString3");
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

            var factory = provider.GetRequiredService<FakeQueueClientFactory>();
            var clientMocks = factory.GetAllRegisteredQueueClients();

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
        public async Task FailsSilentlyIfAQueueClientDoesNotCloseProperlyOnShutdown()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options.RegisterQueue("testQueue").WithConnectionString("testConnectionString");
                    options.RegisterQueue("testQueue2").WithConnectionString("testConnectionString2");
                    options.RegisterQueue("testQueue3").WithConnectionString("testConnectionString3");
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

            var factory = provider.GetRequiredService<FakeQueueClientFactory>();
            var clientMocks = factory.GetAllRegisteredQueueClients();

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
        public async Task CustomMessageHandlerCanReceiveMessages()
        {
            var composer = new ServiceBusComposer();

            var fakeMessageHandler = new FakeMessageHandler();
            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(fakeMessageHandler);
                    services.ConfigureServiceBus(
                        options =>
                        {
                            options
                                .RegisterQueue("testQueue")
                                .WithConnectionString("connectionStringTest")
                                .WithCustomMessageHandler<FakeMessageHandler>();
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var clientMock = provider.GetQueueClientMock("testQueue");

            var sentMessage = new Message();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            fakeMessageHandler.Mock
                .Verify(
                    o => o.HandleMessageAsync(
                        It.Is<MessageContext>(
                            context => context.Message == sentMessage
                                       && context.Receiver.Name == "testQueue"
                                       && context.Token == sentToken)),
                    Times.Once);
        }

        [Fact]
        public async Task CustomMessageHandlerWontReceiveMessagesWhenDeactivated()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddServiceBus(true, false);
            services.OverrideClientFactories();
            var fakeMessageHandler = new FakeMessageHandler();
            services.AddSingleton(fakeMessageHandler);
            services.ConfigureServiceBus(
                options =>
                {
                    options
                        .RegisterQueue("testQueue")
                        .WithConnectionString("connectionStringTest")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());

            var clientMock = provider.GetQueueClientMock("testQueue");

            var sentMessage = new Message();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            fakeMessageHandler.Mock
                .Verify(
                    o => o.HandleMessageAsync(
                        It.Is<MessageContext>(
                            context => context.Message == sentMessage
                                       && context.Receiver.Name == "testQueue"
                                       && context.Token == sentToken)),
                    Times.Never);
        }
    }
}
