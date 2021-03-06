﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class SubscriptionConfigurationTest
    {
        [Fact]
        public async Task CannotRegisterTwoSubscriptionWithTheSameName()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusSubscription("testTopic", "testsubscription001").WithCustomMessageHandler<FakeMessageHandler>();
                services.RegisterServiceBusSubscription("testTopic", "testsubscription001").WithCustomMessageHandler<FakeMessageHandler>();
            });

            await Assert.ThrowsAnyAsync<DuplicateReceiverRegistrationException>(async () => await composer.Compose());
        }

        [Fact]
        public async Task CanRegisterSubscriptions()
        {
            var composer = new Composer();

            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            composer.WithAdditionalServices(services =>
            {
                services.AddSingleton(mock);
                services.RegisterServiceBusSubscription("testTopic", "testsubscription001")
                    .WithConnection("testConnectionString")
                    .WithCustomMessageHandler<FakeMessageHandler>();
                services.RegisterServiceBusSubscription("testTopic", "testsubscription002")
                    .WithConnection("testConnectionString")
                    .WithCustomMessageHandler<FakeMessageHandler>();
            });

            var provider = await composer.Compose();

            var clients = provider.GetRequiredService<FakeSubscriptionClientFactory>().GetAllRegisteredClients();

            var message = new Message();
            foreach (var client in clients)
            {
                await client.TriggerMessageReception(message, CancellationToken.None);
            }

            mock.Verify(
                o => o.HandleMessageAsync(
                    It.Is<MessageContext>(
                        context => context.Message == message
                                   && context.Receiver.Name == "testTopic/Subscriptions/testsubscription001")),
                Times.Once);
            mock.Verify(
                o => o.HandleMessageAsync(
                    It.Is<MessageContext>(
                        context => context.Message == message
                                   && context.Receiver.Name == "testTopic/Subscriptions/testsubscription002")),
                Times.Once);
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithoutRegisteringTopic()
        {
            var composer = new Composer();

            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Callback(
                    (MessageContext context) =>
                    {
                        Assert.Equal("testTopic/Subscriptions/testsubscription001", context.Receiver.Name);
                    })
                .Returns(Task.CompletedTask)
                .Verifiable();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusSubscription("testTopic", "testsubscription001")
                    .WithConnection("testConnectionString")
                    .WithCustomMessageHandler<FakeMessageHandler>();
                services.AddSingleton(mock);
            });

            var provider = await composer.Compose();

            var factory = provider.GetRequiredService<FakeSubscriptionClientFactory>();
            var client = factory.GetAllRegisteredClients().First();
            await client.TriggerMessageReception(new Message(), CancellationToken.None);
            mock.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithConnection()
        {
            var composer = new Composer();

            var serviceBusConnection = new ServiceBusConnection(
                "Endpoint=sb://labepdvsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TOEhvlmrOoLjHfxhYJ3xjoLtVZrMQLqP8MUwrv5flOA=");
            var factory = new Mock<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            factory
                .Setup(o => o.Create(It.IsAny<SubscriptionOptions>(), It.Is<ConnectionSettings>(conn => conn.Connection == serviceBusConnection)))
                .Returns((SubscriptionOptions o, ConnectionSettings p) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection(serviceBusConnection)
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithConnectionString()
        {
            var composer = new Composer();

            var factory = new Mock<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            factory
                .Setup(o => o.Create(It.IsAny<SubscriptionOptions>(), It.Is<ConnectionSettings>(conn => conn.ConnectionString == "testConnectionString")))
                .Returns((SubscriptionOptions o, ConnectionSettings p) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("testConnectionString")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithConnectionStringBuilder()
        {
            var composer = new Composer();

            var connectionStringBuilder = new ServiceBusConnectionStringBuilder();
            var factory = new Mock<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            factory
                .Setup(
                    o => o.Create(It.IsAny<SubscriptionOptions>(), It.Is<ConnectionSettings>(conn => conn.ConnectionStringBuilder == connectionStringBuilder)))
                .Returns((SubscriptionOptions o, ConnectionSettings p) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection(connectionStringBuilder)
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithReceiveMode()
        {
            var composer = new Composer();

            var factory = new Mock<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            factory
                .Setup(o => o.Create(It.IsAny<SubscriptionOptions>(), It.Is<ConnectionSettings>(conn => conn.ReceiveMode == ReceiveMode.ReceiveAndDelete)))
                .Returns((SubscriptionOptions o, ConnectionSettings p) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("testConnectionString", ReceiveMode.ReceiveAndDelete)
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithRetryPolicy()
        {
            var composer = new Composer();

            var retryPolicy = new NoRetry();
            var factory = new Mock<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            factory
                .Setup(o => o.Create(It.IsAny<SubscriptionOptions>(), It.Is<ConnectionSettings>(conn => conn.RetryPolicy == retryPolicy)))
                .Returns((SubscriptionOptions o, ConnectionSettings p) => new SubscriptionClientMock("testSubscription").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("testConnectionString", ReceiveMode.PeekLock, retryPolicy)
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
        }

        [Fact]
        public async Task FailsSilentlyWhenRegisteringQueueWithNoConnectionAndNoDefaultConnection()
        {
            var composer = new Composer();

            composer.WithDefaultSettings(settings => { });
            var logger = new Mock<ILogger<ReceiverWrapper>>();
            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(logger.Object);
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            await composer.Compose();

            logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<MissingConnectionException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UsesDefaultConnectionWhenNoConnectionIsProvided()
        {
            var composer = new Composer();
            var factory = new Mock<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<SubscriptionOptions>(opts => opts.ResourceId == "testTopic/Subscriptions/testSubscription"),
                        It.Is<ConnectionSettings>(conn => conn.ConnectionString == "testConnectionStringFromDefault")))
                .Returns((SubscriptionOptions o, ConnectionSettings p) => new SubscriptionClientMock("testTopic/Subscriptions/testSubscription").Client)
                .Verifiable();
            composer.WithDefaultSettings(
                settings =>
                {
                    settings.WithConnection("testConnectionStringFromDefault");
                });
            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            await composer.Compose();

            factory.VerifyAll();
        }

        [Fact]
        public async Task OverridesDefaultConnectionWhenConcreteConnectionIsProvided()
        {
            var composer = new Composer();
            var factory = new Mock<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<SubscriptionOptions>(opts => opts.ResourceId == "testTopic/Subscriptions/testSubscription"),
                        It.Is<ConnectionSettings>(conn => conn.ConnectionString == "concreteTestConnectionString")))
                .Returns((SubscriptionOptions o, ConnectionSettings p) => new SubscriptionClientMock("testTopic/Subscriptions/testSubscription").Client)
                .Verifiable();
            composer.WithDefaultSettings(
                settings =>
                {
                    settings.WithConnection("testConnectionStringFromDefault");
                });
            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("concreteTestConnectionString")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.Compose();

            factory.VerifyAll();
        }
    }
}
