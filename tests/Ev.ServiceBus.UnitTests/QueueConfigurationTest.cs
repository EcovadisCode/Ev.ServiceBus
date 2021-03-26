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
    public class QueueConfigurationTest
    {
        [Fact]
        public async Task CannotRegisterTwoQueuesWithTheSameName()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusQueue("testQueue");
                    services.RegisterServiceBusQueue("testQueue");
                });

            await Assert.ThrowsAnyAsync<DuplicateSenderRegistrationException>(
                async () => await composer.Compose());
        }

        [Fact]
        public async Task CanRegisterAndRetrieveQueues()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusQueue("testQueue").WithConnection("testConnectionString");
                    services.RegisterServiceBusQueue("testQueue2").WithConnection("testConnectionString2");
                    services.RegisterServiceBusQueue("testQueue3").WithConnection("testConnectionString3");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
            Assert.Equal("testQueue2", registry.GetQueueSender("testQueue2")?.Name);
            Assert.Equal("testQueue3", registry.GetQueueSender("testQueue3")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithConnection()
        {
            var composer = new Composer();

            var serviceBusConnection = new ServiceBusConnection(
                "Endpoint=sb://labepdvsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TOEhvlmrOoLjHfxhYJ3xjoLtVZrMQLqP8MUwrv5flOA=");
            var factory = new Mock<IClientFactory<QueueOptions, IQueueClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<QueueOptions>(opts => opts.ResourceId == "testQueue"),
                        It.Is<ConnectionSettings>(conn => conn.Connection == serviceBusConnection)))
                .Returns((QueueOptions o, ConnectionSettings p) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusQueue("testQueue").WithConnection(serviceBusConnection);
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithConnectionString()
        {
            var composer = new Composer();

            var factory = new Mock<IClientFactory<QueueOptions, IQueueClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<QueueOptions>(opts => opts.ResourceId == "testQueue"),
                        It.Is<ConnectionSettings>(conn => conn.ConnectionString == "testConnectionString")))
                .Returns((QueueOptions o, ConnectionSettings p) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusQueue("testQueue").WithConnection("testConnectionString");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithConnectionStringBuilder()
        {
            var composer = new Composer();

            var connectionStringBuilder = new ServiceBusConnectionStringBuilder();
            var factory = new Mock<IClientFactory<QueueOptions, IQueueClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<QueueOptions>(opts => opts.ResourceId == "testQueue"),
                        It.Is<ConnectionSettings>(conn => conn.ConnectionStringBuilder == connectionStringBuilder)))
                .Returns((QueueOptions o, ConnectionSettings p) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusQueue("testQueue").WithConnection(connectionStringBuilder);
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithReceiveMode()
        {
            var composer = new Composer();

            var factory = new Mock<IClientFactory<QueueOptions, IQueueClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<QueueOptions>(opts => opts.ResourceId == "testQueue"),
                        It.Is<ConnectionSettings>(conn => conn.ReceiveMode == ReceiveMode.ReceiveAndDelete)))
                .Returns((QueueOptions o, ConnectionSettings p) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusQueue("testQueue")
                        .WithConnection("testConnectionString", ReceiveMode.ReceiveAndDelete);
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithRetryPolicy()
        {
            var composer = new Composer();

            var retryPolicy = new NoRetry();
            var factory = new Mock<IClientFactory<QueueOptions, IQueueClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<QueueOptions>(opts => opts.ResourceId == "testQueue"),
                        It.Is<ConnectionSettings>(conn => conn.RetryPolicy == retryPolicy)))
                .Returns((QueueOptions o, ConnectionSettings p) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideClientFactory(factory.Object);

                    services.RegisterServiceBusQueue("testQueue")
                        .WithConnection("testConnectionString", ReceiveMode.PeekLock, retryPolicy);
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task FailsSilentlyWhenAnErrorOccursBuildingAQueueClient()
        {
            var composer = new Composer();
            composer.OverrideClientFactory(new FailingClientFactory<QueueOptions, IQueueClient>());
            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusQueue("testQueue").WithConnection("testConnectionString");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<ServiceBusRegistry>();

            await Assert.ThrowsAsync<MessageSenderUnavailableException>(
                async () =>
                {
                    await registry.GetQueueSender("testQueue").SendAsync(new Message());
                });
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
            services.RegisterServiceBusQueue("testQueue").WithConnection("testConnectionString");

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(new CancellationToken());
            var composer = new Composer();
            composer.OverrideClientFactory(new FailingClientFactory<QueueOptions, IQueueClient>());

            var registry = provider.GetService<ServiceBusRegistry>();
            await registry.GetQueueSender("testQueue").SendAsync(new Message());
        }

        [Fact]
        public async Task FailsSilentlyWhenRegisteringQueueWithNoConnectionAndNoDefaultConnection()
        {
            var composer = new Composer();

            var logger = new Mock<ILogger<SenderWrapper>>();
            composer.WithDefaultSettings(settings => {});
            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(logger.Object);
                    services.RegisterServiceBusQueue("testQueue");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();
            await Assert.ThrowsAsync<MessageSenderUnavailableException>(
                async () =>
                {
                    await registry.GetQueueSender("testQueue").SendAsync(new Message());
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
            var factory = new Mock<IClientFactory<QueueOptions, IQueueClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<QueueOptions>(opts => opts.ResourceId == "testQueue"),
                        It.Is<ConnectionSettings>(conn => conn.ConnectionString == "testConnectionStringFromDefault")))
                .Returns((QueueOptions o, ConnectionSettings p) => new QueueClientMock("testQueue").QueueClient)
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
                    services.RegisterServiceBusQueue("testQueue");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task OverridesDefaultConnectionWhenConcreteConnectionIsProvided()
        {
            var composer = new Composer();
            var factory = new Mock<IClientFactory<QueueOptions, IQueueClient>>();
            factory
                .Setup(
                    o => o.Create(
                        It.Is<QueueOptions>(opts => opts.ResourceId == "testQueue"),
                        It.Is<ConnectionSettings>(conn => conn.ConnectionString == "concreteTestConnectionString")))
                .Returns((QueueOptions o, ConnectionSettings p) => new QueueClientMock("testQueue").QueueClient)
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
                    services.RegisterServiceBusQueue("testQueue").WithConnection("concreteTestConnectionString");
                });

            var provider = await composer.Compose();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }
    }
}
