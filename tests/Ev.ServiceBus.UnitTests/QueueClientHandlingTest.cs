using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
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
                services.RegisterServiceBusQueue("testQueue").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                services.RegisterServiceBusQueue("testQueue2").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
                services.RegisterServiceBusQueue("testQueue3").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
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
                services.RegisterServiceBusQueue("testQueue").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                services.RegisterServiceBusQueue("testQueue2").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
                services.RegisterServiceBusQueue("testQueue3").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
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
                services.RegisterServiceBusQueue("testQueue").WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                services.RegisterServiceBusQueue("testQueue2").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
                services.RegisterServiceBusQueue("testQueue3").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
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
                        .WithConnection("Endpoint=connectionStringTest;", new ServiceBusClientOptions())
                        .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                });

            var provider = await composer.Compose();

            var clientMock = composer.ClientFactory.GetProcessorMock("testQueue");

            var sentMessage = new ServiceBusMessage();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            mock.Verify(o => o.HandleMessageAsync(
                        It.Is<MessageContext>(context => context.Message.MessageId == sentMessage.MessageId)),
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
            services.OverrideClientFactory();
            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            services.AddSingleton(mock);
            services.RegisterServiceBusQueue("testQueue")
                .WithConnection("Endpoint=connectionStringTest;", new ServiceBusClientOptions())
                .WithCustomMessageHandler<FakeMessageHandler>(_ => { });

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());

            var factory = provider.GetRequiredService<FakeClientFactory>();
            var receivers = factory.GetAllProcessorMocks();
            receivers.Length.Should().Be(0);

            // var sender = factory.GetSenderMock("testQueue");
            // var sentMessage = new Message();
            // var sentToken = new CancellationToken();
            // await sender.TriggerMessageReception(sentMessage, sentToken);

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
