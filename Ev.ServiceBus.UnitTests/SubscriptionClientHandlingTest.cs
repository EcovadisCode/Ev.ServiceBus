using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
    public class SubscriptionClientHandlingTest
    {
        [Fact]
        public async Task ClosesTheSubscriptionClientsProperlyOnShutdown()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options.RegisterSubscription("testtopic1", "testsub1").WithConnectionString("testConnectionString1");
                    options.RegisterSubscription("testtopic2", "testsub1").WithConnectionString("testConnectionString2");
                    options.RegisterSubscription("testtopic3", "testsub1").WithConnectionString("testConnectionString3");
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
            var clientMocks = factory.GetAllRegisteredSubscriptionClients();

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
        public async Task FailsSilentlyIfASubscriptionClientDoesNotCloseProperlyOnShutdown()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options.RegisterSubscription("testtopic1", "testsub1").WithConnectionString("testConnectionString1");
                    options.RegisterSubscription("testtopic2", "testsub1").WithConnectionString("testConnectionString2");
                    options.RegisterSubscription("testtopic3", "testsub1").WithConnectionString("testConnectionString3");
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
            var clientMocks = factory.GetAllRegisteredSubscriptionClients();

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
                                .RegisterSubscription("testTopic", "testSub")
                                .WithConnectionString("connectionStringTest")
                                .WithCustomMessageHandler<FakeMessageHandler>();
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var clientMock = provider.GetSubscriptionClientMock("testSub");

            var sentMessage = new Message();
            var sentToken = new CancellationToken();
            await clientMock.TriggerMessageReception(sentMessage, sentToken);

            fakeMessageHandler.Mock
                .Verify(
                    o => o.HandleMessageAsync(
                        It.Is<MessageContext>(
                            context => context.Message == sentMessage
                                       && context.Receiver.Name == "testTopic/Subscriptions/testSub"
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
                        .RegisterSubscription("testTopic", "testSub")
                        .WithConnectionString("connectionStringTest")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());

            var clientMock = provider.GetSubscriptionClientMock("testSub");

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
