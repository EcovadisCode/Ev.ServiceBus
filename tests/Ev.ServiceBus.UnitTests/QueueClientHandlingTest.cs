using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
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
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("testQueue").WithConnection("testConnectionString");
                services.RegisterServiceBusQueue("testQueue2").WithConnection("testConnectionString2");
                services.RegisterServiceBusQueue("testQueue3").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeQueueClientFactory>();
            var clientMocks = factory.GetAllRegisteredClients();

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
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("testQueue").WithConnection("testConnectionString");
                services.RegisterServiceBusQueue("testQueue2").WithConnection("testConnectionString2");
                services.RegisterServiceBusQueue("testQueue3").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeQueueClientFactory>();
            var clientMocks = factory.GetAllRegisteredClients();

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
        public async Task DontCallCloseWhenTheQueueClientIsAlreadyClosing()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("testQueue").WithConnection("testConnectionString");
                services.RegisterServiceBusQueue("testQueue2").WithConnection("testConnectionString2");
                services.RegisterServiceBusQueue("testQueue3").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeQueueClientFactory>();
            var clientMocks = factory.GetAllRegisteredClients();

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
        public async Task CustomMessageHandlerCanReceiveMessages()
        {
            var composer = new Composer();

            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(mock);
                    services.RegisterServiceBusQueue("testQueue")
                        .WithConnection("connectionStringTest")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.Compose();

            var clientMock = provider.GetQueueClientMock("testQueue");

            var sentMessage = new Message();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            mock
                .Verify(
                    o => o.HandleMessageAsync(
                        It.Is<MessageContext>(
                            context => context.Message == sentMessage
                                       && context.Receiver.Name == "testQueue"
                                       && context.Receiver.ClientType == ClientType.Queue
                                       && context.Token == sentToken)),
                    Times.Once);
        }

        [Fact]
        public async Task CustomMessageHandlerWontReceiveMessagesWhenDeactivated()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddServiceBus<PayloadSerializer>(
                settings =>
                {
                    settings.ReceiveMessages = false;
                });
            services.OverrideClientFactories();
            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            services.AddSingleton(mock);
            services.RegisterServiceBusQueue("testQueue")
                .WithConnection("connectionStringTest")
                .WithCustomMessageHandler<FakeMessageHandler>();

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());

            var factory = provider.GetRequiredService<FakeQueueClientFactory>();
            var receivers = factory.GetAllRegisteredClients().Where(o => o.IsReceiver).ToArray();
            receivers.Length.Should().Be(0);

            var senders = factory.GetAllRegisteredClients().Where(o => o.IsReceiver == false).ToArray();
            foreach (var sender in senders)
            {
                var sentMessage = new Message();
                var sentToken = new CancellationToken();
                await sender.TriggerMessageReception(sentMessage, sentToken);
            }

            mock.Verify(
                    o => o.HandleMessageAsync(It.IsAny<MessageContext>()),
                    Times.Never);
        }

        [Fact]
        public async Task ThrowsExceptionWhenAQueueSenderIsNotFound()
        {
            var composer = new Composer();

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            var ex = Assert.Throws<QueueSenderNotFoundException>(
                () =>
                {
                    registry.GetQueueSender("notARegisteredQueueName");
                });
            ex.QueueName.Should().Be("notARegisteredQueueName");
        }

    }
}
