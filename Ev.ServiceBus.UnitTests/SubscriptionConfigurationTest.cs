using System;
using System.Linq;
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
    public class SubscriptionConfigurationTest
    {
        [Fact]
        public async Task CannotRegisterTwoSubscriptionWithTheSameName()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options
                        .RegisterSubscription("testTopic", "testsubscription001")
                        .WithConnectionString("testConnectionString");
                    options
                        .RegisterSubscription("testTopic", "testsubscription001")
                        .WithConnectionString("testConnectionString");
                });
            });

            await Assert.ThrowsAnyAsync<DuplicateSubscriptionRegistrationException>(async () => await composer.ComposeAndSimulateStartup());
        }

        [Fact]
        public async Task CanRegisterSubscriptions()
        {
            var composer = new ServiceBusComposer();

            var fakeMessageHandler = new FakeMessageHandler();

            composer.WithAdditionalServices(services =>
            {
                services.AddSingleton(fakeMessageHandler);
                services.ConfigureServiceBus(options =>
                {
                    options
                        .RegisterSubscription("testTopic", "testsubscription001")
                        .WithConnectionString("testConnectionString")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                    options
                        .RegisterSubscription("testTopic", "testsubscription002")
                        .WithConnectionString("testConnectionString")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

            var clients = provider.GetRequiredService<FakeSubscriptionClientFactory>().GetAllRegisteredSubscriptionClients();

            var message = new Message();
            foreach (var client in clients)
            {
                await client.TriggerMessageReception(message, CancellationToken.None);
            }

            fakeMessageHandler.Mock.Verify(
                o => o.HandleMessageAsync(
                    It.Is<MessageContext>(
                        context => context.Message == message
                                   && context.Receiver.Name == "testTopic/Subscriptions/testsubscription001")),
                Times.Once);
            fakeMessageHandler.Mock.Verify(
                o => o.HandleMessageAsync(
                    It.Is<MessageContext>(
                        context => context.Message == message
                                   && context.Receiver.Name == "testTopic/Subscriptions/testsubscription002")),
                Times.Once);
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithoutRegisteringTopic()
        {
            Task OnMessageReceived(MessageContext context)
            {
                Assert.Equal("testTopic/Subscriptions/testsubscription001", context.Receiver.Name);
                return Task.CompletedTask;
            }

            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options
                        .RegisterSubscription("testTopic", "testsubscription001")
                        .WithConnectionString("testConnectionString")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });
                services.AddSingleton(new FakeMessageHandler(OnMessageReceived));
            });

            var provider = await composer.ComposeAndSimulateStartup();

            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
            var client = factory.GetAllRegisteredSubscriptionClients().First();
            await client.TriggerMessageReception(new Message(), CancellationToken.None);
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithConnection()
        {
            var composer = new ServiceBusComposer();

            var serviceBusConnection = new ServiceBusConnection(
                "Endpoint=sb://labepdvsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TOEhvlmrOoLjHfxhYJ3xjoLtVZrMQLqP8MUwrv5flOA=");
            var factory = new Mock<ISubscriptionClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<SubscriptionOptions>(opts => opts.Connection == serviceBusConnection)))
                .Returns((SubscriptionOptions o) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideSubscriptionClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterSubscription("testTopic", "testSubscription").WithConnection(serviceBusConnection);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithConnectionString()
        {
            var composer = new ServiceBusComposer();

            var factory = new Mock<ISubscriptionClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<SubscriptionOptions>(opts => opts.ConnectionString == "testConnectionString")))
                .Returns((SubscriptionOptions o) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideSubscriptionClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterSubscription("testTopic", "testSubscription").WithConnectionString("testConnectionString");
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithConnectionStringBuilder()
        {
            var composer = new ServiceBusComposer();

            var connectionStringBuilder = new ServiceBusConnectionStringBuilder();
            var factory = new Mock<ISubscriptionClientFactory>();
            factory
                .Setup(
                    o => o.Create(It.Is<SubscriptionOptions>(opts => opts.ConnectionStringBuilder == connectionStringBuilder)))
                .Returns((SubscriptionOptions o) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideSubscriptionClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterSubscription("testTopic", "testSubscription").WithConnectionStringBuilder(connectionStringBuilder);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithReceiveMode()
        {
            var composer = new ServiceBusComposer();

            var factory = new Mock<ISubscriptionClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<SubscriptionOptions>(opts => opts.ReceiveMode == ReceiveMode.ReceiveAndDelete)))
                .Returns((SubscriptionOptions o) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideSubscriptionClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterSubscription("testTopic", "testSubscription")
                                .WithConnectionString("testConnectionString")
                                .WithReceiveMode(ReceiveMode.ReceiveAndDelete);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithRetryPolicy()
        {
            var composer = new ServiceBusComposer();

            var retryPolicy = new NoRetry();
            var factory = new Mock<ISubscriptionClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<SubscriptionOptions>(opts => opts.RetryPolicy == retryPolicy)))
                .Returns((SubscriptionOptions o) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideSubscriptionClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterSubscription("testTopic", "testSubscription")
                                .WithConnectionString("testConnectionString")
                                .WithRetryPolicy(retryPolicy);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }
    }
}
