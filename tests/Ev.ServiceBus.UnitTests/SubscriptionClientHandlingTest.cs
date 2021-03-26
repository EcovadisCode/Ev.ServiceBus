using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class SubscriptionClientHandlingTest
    {
        [Fact]
        public async Task ClosesTheSubscriptionClientsProperlyOnShutdown()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusSubscription("testtopic1", "testsub1").WithConnection("testConnectionString1");
                services.RegisterServiceBusSubscription("testtopic2", "testsub1").WithConnection("testConnectionString2");
                services.RegisterServiceBusSubscription("testtopic3", "testsub1").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
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

        public class MessageHandler : IMessageHandler
        {
            public Task HandleMessageAsync(MessageContext context)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task FailsSilentlyIfASubscriptionClientDoesNotCloseProperlyOnShutdown()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusSubscription("testtopic1", "testsub1").WithCustomMessageHandler<MessageHandler>();
                services.RegisterServiceBusSubscription("testtopic2", "testsub1").WithCustomMessageHandler<MessageHandler>();
                services.RegisterServiceBusSubscription("testtopic3", "testsub1").WithCustomMessageHandler<MessageHandler>();
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
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
        public async Task DontCallCloseWhenTheSubscriptionClientIsAlreadyClosing()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusSubscription("testtopic1", "testsub1").WithConnection("testConnectionString1");
                services.RegisterServiceBusSubscription("testtopic2", "testsub1").WithConnection("testConnectionString2");
                services.RegisterServiceBusSubscription("testtopic3", "testsub1").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
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
                    services.RegisterServiceBusSubscription("testTopic", "testSub")
                        .WithConnection("connectionStringTest")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.Compose();

            var clientMock = provider.GetSubscriptionClientMock("testSub");

            var sentMessage = new Message();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            mock
                .Verify(
                    o => o.HandleMessageAsync(
                        It.Is<MessageContext>(
                            context => context.Message == sentMessage
                                       && context.Receiver.Name == "testTopic/Subscriptions/testSub"
                                       && context.Receiver.ClientType == ClientType.Subscription
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
                    settings.Enabled = true;
                    settings.ReceiveMessages = false;
                });
            services.OverrideClientFactories();
            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            services.AddSingleton(mock);
            services.RegisterServiceBusSubscription("testTopic", "testSub")
                .WithConnection("connectionStringTest")
                .WithCustomMessageHandler<FakeMessageHandler>();

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());

            var clientMock = provider.GetSubscriptionClientMock("testSub");

            var sentMessage = new Message();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            mock
                .Verify(
                    o => o.HandleMessageAsync(
                        It.Is<MessageContext>(
                            context => context.Message == sentMessage
                                       && context.Receiver.Name == "testQueue"
                                       && context.Receiver.ClientType == ClientType.Subscription
                                       && context.Token == sentToken)),
                    Times.Never);
        }
    }
}
