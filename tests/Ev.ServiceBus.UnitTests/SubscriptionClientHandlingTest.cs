using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class SubscriptionClientHandlingTest
    {
        // [Fact]
        // public async Task ClosesTheSubscriptionClientsProperlyOnShutdown()
        // {
        //     var composer = new Composer();
        //
        //     composer.WithAdditionalServices(services =>
        //     {
        //         services.RegisterServiceBusSubscription("testtopic1", "testsub1").WithConnection("Endpoint=testConnectionString1;", new ServiceBusClientOptions());
        //         services.RegisterServiceBusSubscription("testtopic2", "testsub1").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
        //         services.RegisterServiceBusSubscription("testtopic3", "testsub1").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
        //     });
        //
        //     var provider = await composer.Compose();
        //
        //     var clientMocks = composer.ClientFactory.GetAllProcessorMocks();
        //     foreach (var clientMock in clientMocks)
        //     {
        //         clientMock.Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        //     }
        //
        //     await provider.SimulateStopHost(token: new CancellationToken());
        //
        //     foreach (var clientMock in clientMocks)
        //     {
        //         clientMock.Mock.Verify(o => o.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
        //     }
        // }

        public class MessageHandler : IMessageHandler
        {
            public Task HandleMessageAsync(MessageContext context)
            {
                return Task.CompletedTask;
            }
        }

        // [Fact]
        // public async Task FailsSilentlyIfASubscriptionClientDoesNotCloseProperlyOnShutdown()
        // {
        //     var composer = new Composer();
        //
        //     composer.WithAdditionalServices(services =>
        //     {
        //         services.RegisterServiceBusSubscription("testtopic1", "testsub1").WithCustomMessageHandler<MessageHandler>(_ => {});
        //         services.RegisterServiceBusSubscription("testtopic2", "testsub1").WithCustomMessageHandler<MessageHandler>(_ => {});
        //         services.RegisterServiceBusSubscription("testtopic3", "testsub1").WithCustomMessageHandler<MessageHandler>(_ => {});
        //     });
        //
        //     var provider = await composer.Compose();
        //
        //     var clientMocks = composer.ClientFactory.GetAllProcessorMocks();
        //
        //     clientMocks[0].Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        //     clientMocks[1].Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Throws<SocketException>().Verifiable();
        //     clientMocks[2].Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        //
        //     await provider.SimulateStopHost(token: new CancellationToken());
        //
        //     foreach (var clientMock in clientMocks)
        //     {
        //         clientMock.Mock.Verify(o => o.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
        //     }
        // }

        // [Fact]
        // public async Task DontCallCloseWhenTheSubscriptionClientIsAlreadyClosing()
        // {
        //     var composer = new Composer();
        //
        //     composer.WithAdditionalServices(services =>
        //     {
        //         services.RegisterServiceBusSubscription("testtopic1", "testsub1").WithConnection("Endpoint=testConnectionString1;", new ServiceBusClientOptions());
        //         services.RegisterServiceBusSubscription("testtopic2", "testsub1").WithConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
        //         services.RegisterServiceBusSubscription("testtopic3", "testsub1").WithConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
        //     });
        //
        //     var provider = await composer.Compose();
        //
        //     var clientMocks = composer.ClientFactory.GetAllProcessorMocks();
        //
        //     foreach (var clientMock in clientMocks)
        //     {
        //         clientMock.Mock.SetupGet(o => o.IsClosed).Returns(true);
        //         clientMock.Mock.Setup(o => o.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        //     }
        //
        //     await provider.SimulateStopHost(token: new CancellationToken());
        //
        //     foreach (var clientMock in clientMocks)
        //     {
        //         clientMock.Mock.Verify(o => o.CloseAsync(It.IsAny<CancellationToken>()), Times.Never);
        //     }
        // }

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
                    services.RegisterServiceBusSubscription("testTopic", "testSub")
                        .WithConnection("Endpoint=connectionStringTest;", new ServiceBusClientOptions())
                        .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                });

            var provider = await composer.Compose();

            var clientMock = composer.ClientFactory.GetProcessorMock("testTopic", "testSub");

            var sentMessage = new ServiceBusMessage();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            mock.Verify(o => o.HandleMessageAsync(It.Is<MessageContext>(context => context.Message.MessageId == sentMessage.MessageId)),Times.Once);
        }

        [Fact]
        public async Task CustomMessageHandlerWontReceiveMessagesWhenDeactivated()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddServiceBus<PayloadSerializer>(
                settings =>
                {
                    settings.Enabled = true;
                    settings.ReceiveMessages = false;
                });
            services.OverrideClientFactory();
            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            services.AddSingleton(mock);
            services.RegisterServiceBusSubscription("testTopic", "testSub")
                .WithConnection("Endpoint=connectionStringTest;", new ServiceBusClientOptions())
                .WithCustomMessageHandler<FakeMessageHandler>(_ => {});

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());

            var clientMock = provider.GetProcessorMock("testTopic", "testSub");

            var sentMessage = new ServiceBusMessage();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            mock.Verify(o => o.HandleMessageAsync(It.Is<MessageContext>(context => context.Message.MessageId == sentMessage.MessageId)),Times.Never);
        }
    }
}
