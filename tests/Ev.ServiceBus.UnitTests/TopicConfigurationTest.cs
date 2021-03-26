using System;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class TopicConfigurationTest
    {
        [Fact]
        public async Task CannotRegisterTwoTopicsWithTheSameName()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("testTopic");
                services.RegisterServiceBusTopic("testTopic");
            });

            await Assert.ThrowsAnyAsync<DuplicateSenderRegistrationException>(async () => await composer.Compose());
        }

        [Fact]
        public async Task CanRegisterAndRetrieveTopics()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("testTopic").WithConnection("testConnectionString");
                services.RegisterServiceBusTopic("testTopic2").WithConnection("testConnectionString2");
                services.RegisterServiceBusTopic("testTopic3").WithConnection("testConnectionString3");
            });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
            Assert.Equal("testTopic2", registry.GetTopicSender("testTopic2")?.Name);
            Assert.Equal("testTopic3", registry.GetTopicSender("testTopic3")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithConnection()
        {
            var composer = new Composer();

            var serviceBusConnection = new ServiceBusConnection(
                "Endpoint=sb://labepdvsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TOEhvlmrOoLjHfxhYJ3xjoLtVZrMQLqP8MUwrv5flOA=");
            var factory = new Mock<IClientFactory<TopicOptions, ITopicClient>>();
            factory
                .Setup(o => o.Create(It.IsAny<TopicOptions>(), It.Is<ConnectionSettings>(conn => conn.Connection == serviceBusConnection)))
                .Returns((TopicOptions o, ConnectionSettings p) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusTopic("testTopic").WithConnection(serviceBusConnection);
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithConnectionString()
        {
            var composer = new Composer();

            var factory = new Mock<IClientFactory<TopicOptions, ITopicClient>>();
            factory
                .Setup(o => o.Create(It.IsAny<TopicOptions>(), It.Is<ConnectionSettings>(conn => conn.ConnectionString == "testConnectionString")))
                .Returns((TopicOptions o, ConnectionSettings p) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusTopic("testTopic").WithConnection("testConnectionString");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithConnectionStringBuilder()
        {
            var composer = new Composer();

            var connectionStringBuilder = new ServiceBusConnectionStringBuilder();
            var factory = new Mock<IClientFactory<TopicOptions, ITopicClient>>();
            factory
                .Setup(
                    o => o.Create(It.IsAny<TopicOptions>(), It.Is<ConnectionSettings>(conn => conn.ConnectionStringBuilder == connectionStringBuilder)))
                .Returns((TopicOptions o, ConnectionSettings p) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusTopic("testTopic").WithConnection(connectionStringBuilder);
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithReceiveMode()
        {
            var composer = new Composer();

            var factory = new Mock<IClientFactory<TopicOptions, ITopicClient>>();
            factory
                .Setup(o => o.Create(It.IsAny<TopicOptions>(), It.Is<ConnectionSettings>(conn => conn.ReceiveMode == ReceiveMode.ReceiveAndDelete)))
                .Returns((TopicOptions o, ConnectionSettings p) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusTopic("testTopic")
                        .WithConnection("testConnectionString", ReceiveMode.ReceiveAndDelete);
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithRetryPolicy()
        {
            var composer = new Composer();

            var retryPolicy = new NoRetry();
            var factory = new Mock<IClientFactory<TopicOptions, ITopicClient>>();
            factory
                .Setup(o => o.Create(It.IsAny<TopicOptions>(), It.Is<ConnectionSettings>(conn => conn.RetryPolicy == retryPolicy)))
                .Returns((TopicOptions o, ConnectionSettings p) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusTopic("testTopic")
                        .WithConnection("testConnectionString", ReceiveMode.PeekLock, retryPolicy);
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task DoesntThrowExceptionWhenServiceBusIsDeactivated()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddServiceBus<PayloadSerializer>(
                settings =>
                {
                    settings.Enabled = false;
                });
            services.RegisterServiceBusTopic("testTopic").WithConnection("testConnectionString");

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());
            var composer = new Composer();
            composer.OverrideClientFactory(new FailingClientFactory<TopicOptions, ITopicClient>());

            var registry = provider.GetService<ServiceBusRegistry>();
            await registry.GetTopicSender("testTopic").SendAsync(new Message());
        }

        [Fact]
        public async Task FailsSilentlyWhenRegisteringQueueWithNoConnectionAndNoDefaultConnection()
        {
            var composer = new Composer();
            composer.WithDefaultSettings(settings => { });

            var logger = new Mock<ILogger<SenderWrapper>>();
            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(logger.Object);
                    services.RegisterServiceBusTopic("testTopic");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();
            await Assert.ThrowsAsync<MessageSenderUnavailableException>(
                async () =>
                {
                    await registry.GetTopicSender("testTopic").SendAsync(new Message());
                });
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
            var factory = new Mock<IClientFactory<TopicOptions, ITopicClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<TopicOptions>(opts => opts.ResourceId == "testTopic"),
                        It.Is<ConnectionSettings>(conn => conn.ConnectionString == "testConnectionStringFromDefault")))
                .Returns((QueueOptions o, ConnectionSettings p) => new TopicClientMock("testTopic").Client)
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
                    services.RegisterServiceBusTopic("testTopic");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task OverridesDefaultConnectionWhenConcreteConnectionIsProvided()
        {
            var composer = new Composer();
            var factory = new Mock<IClientFactory<TopicOptions, ITopicClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<TopicOptions>(opts => opts.ResourceId == "testTopic"),
                        It.Is<ConnectionSettings>(conn => conn.ConnectionString == "concreteTestConnectionString")))
                .Returns((QueueOptions o, ConnectionSettings p) => new TopicClientMock("testTopic").Client)
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
                    services.RegisterServiceBusTopic("testTopic").WithConnection("concreteTestConnectionString");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }
    }
}
