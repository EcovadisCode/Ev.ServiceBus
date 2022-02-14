using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
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
                services.RegisterServiceBusSubscription("testTopic", "testsubscription001").WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                services.RegisterServiceBusSubscription("testTopic", "testsubscription001").WithCustomMessageHandler<FakeMessageHandler>(_ => {});
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
                    .WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions())
                    .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                services.RegisterServiceBusSubscription("testTopic", "testsubscription002")
                    .WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions())
                    .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
            });

            var provider = await composer.Compose();

            var clients = composer.ClientFactory.GetAllProcessorMocks();

            var message = new ServiceBusMessage();
            foreach (var client in clients)
            {
                await client.TriggerMessageReception(message, CancellationToken.None);
            }

            mock.Verify(o => o.HandleMessageAsync(It.Is<MessageContext>(context =>
                    context.Message.MessageId == message.MessageId
                    && context.ResourceId == "testTopic/Subscriptions/testsubscription001")),
                Times.Once);
            mock.Verify(o => o.HandleMessageAsync(It.Is<MessageContext>(context =>
                    context.Message.MessageId == message.MessageId
                    && context.ResourceId == "testTopic/Subscriptions/testsubscription002")),
                Times.Once);
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithoutRegisteringTopic()
        {
            var composer = new Composer();

            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Callback((MessageContext context) =>
                {
                    Assert.Equal("testTopic/Subscriptions/testsubscription001", context.ResourceId);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusSubscription("testTopic", "testsubscription001")
                    .WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions())
                    .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                services.AddSingleton(mock);
            });

            var provider = await composer.Compose();

            var client = composer.ClientFactory.GetProcessorMock("testTopic", "testsubscription001");
            await client.TriggerMessageReception(new ServiceBusMessage(), CancellationToken.None);
            mock.VerifyAll();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithConnectionString()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions())
                        .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();
            composer.ClientFactory.GetProcessorMock("testTopic", "testSubscription").Should().NotBeNull();
        }

        [Fact]
        public async Task CanRegisterSubscriptionWithReceiveMode()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions())
                        .WithCustomMessageHandler<FakeMessageHandler>(options =>
                        {
                            options.ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete;
                        });
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            var client = composer.ClientFactory.GetProcessorMock("testTopic", "testSubscription");
            client.Options.ReceiveMode.Should().Be(ServiceBusReceiveMode.ReceiveAndDelete);
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
                        .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
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
            composer.WithDefaultSettings(
                settings =>
                {
                    settings.WithConnection("Endpoint=testConnectionStringFromDefault;", new ServiceBusClientOptions());
                });
            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                });

            await composer.Compose();

            var client = composer.ClientFactory.GetProcessorMock("testTopic", "testSubscription");

            client.Should().BeNull();
        }

        [Fact]
        public async Task OverridesDefaultConnectionWhenConcreteConnectionIsProvided()
        {
            var composer = new Composer();
            composer.WithDefaultSettings(
                settings =>
                {
                    settings.WithConnection("Endpoint=testConnectionStringFromDefault", new ServiceBusClientOptions());
                });
            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("Endpoint=concreteTestConnectionString;", new ServiceBusClientOptions())
                        .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                });

            var provider = await composer.Compose();

            var client = composer.ClientFactory.GetAssociatedMock("concreteTestConnectionString");
            client.GetProcessorMock("testTopic", "testSubscription").Should().NotBeNull();
        }
    }
}
