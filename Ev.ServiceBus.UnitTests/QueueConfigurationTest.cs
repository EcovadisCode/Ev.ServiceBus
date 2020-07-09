using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Exceptions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class QueueConfigurationTest
    {
        [Fact]
        public async Task CannotRegisterTwoQueuesWithTheSameName()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterQueue("testQueue");
                            options.RegisterQueue("testQueue");
                        });
                });

            await Assert.ThrowsAnyAsync<DuplicateQueueRegistrationException>(
                async () => await composer.ComposeAndSimulateStartup());
        }

        [Fact]
        public async Task CanRegisterAndRetrieveQueues()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterQueue("testQueue").WithConnectionString("testConnectionString");
                            options.RegisterQueue("testQueue2").WithConnectionString("testConnectionString2");
                            options.RegisterQueue("testQueue3").WithConnectionString("testConnectionString3");
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
            Assert.Equal("testQueue2", registry.GetQueueSender("testQueue2")?.Name);
            Assert.Equal("testQueue3", registry.GetQueueSender("testQueue3")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithConnection()
        {
            var composer = new ServiceBusComposer();

            var serviceBusConnection = new ServiceBusConnection(
                "Endpoint=sb://labepdvsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TOEhvlmrOoLjHfxhYJ3xjoLtVZrMQLqP8MUwrv5flOA=");
            var factory = new Mock<IQueueClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<QueueOptions>(opts => opts.Connection == serviceBusConnection)))
                .Returns((QueueOptions o) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideQueueClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterQueue("testQueue").WithConnection(serviceBusConnection);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithConnectionString()
        {
            var composer = new ServiceBusComposer();

            var factory = new Mock<IQueueClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<QueueOptions>(opts => opts.ConnectionString == "testConnectionString")))
                .Returns((QueueOptions o) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideQueueClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterQueue("testQueue").WithConnectionString("testConnectionString");
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithConnectionStringBuilder()
        {
            var composer = new ServiceBusComposer();

            var connectionStringBuilder = new ServiceBusConnectionStringBuilder();
            var factory = new Mock<IQueueClientFactory>();
            factory
                .Setup(
                    o => o.Create(It.Is<QueueOptions>(opts => opts.ConnectionStringBuilder == connectionStringBuilder)))
                .Returns((QueueOptions o) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideQueueClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterQueue("testQueue").WithConnectionStringBuilder(connectionStringBuilder);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithReceiveMode()
        {
            var composer = new ServiceBusComposer();

            var factory = new Mock<IQueueClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<QueueOptions>(opts => opts.ReceiveMode == ReceiveMode.ReceiveAndDelete)))
                .Returns((QueueOptions o) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideQueueClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterQueue("testQueue")
                                .WithConnectionString("testConnectionString")
                                .WithReceiveMode(ReceiveMode.ReceiveAndDelete);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task CanRegisterQueueWithRetryPolicy()
        {
            var composer = new ServiceBusComposer();

            var retryPolicy = new NoRetry();
            var factory = new Mock<IQueueClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<QueueOptions>(opts => opts.RetryPolicy == retryPolicy)))
                .Returns((QueueOptions o) => new QueueClientMock("testQueue").QueueClient)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideQueueClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterQueue("testQueue")
                                .WithConnectionString("testConnectionString")
                                .WithRetryPolicy(retryPolicy);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testQueue", registry.GetQueueSender("testQueue")?.Name);
        }

        [Fact]
        public async Task FailsSilentlyWhenAnErrorOccursBuildingAQueueClient()
        {
            var composer = new ServiceBusComposer();
            composer.OverrideQueueClientFactory(new FailingQueueClientFactory());
            composer.WithAdditionalServices(
                services =>
                {
                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterQueue("testQueue").WithConnectionString("testConnectionString");
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

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
            services.AddServiceBus(false);
            services.ConfigureServiceBus(
                options =>
                {
                    options.RegisterQueue("testQueue").WithConnectionString("testConnectionString");
                });

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());
            var composer = new ServiceBusComposer();
            composer.OverrideQueueClientFactory(new FailingQueueClientFactory());

            var registry = provider.GetService<ServiceBusRegistry>();
            await registry.GetQueueSender("testQueue").SendAsync(new Message());
        }

        public class FailingQueueClientFactory : IQueueClientFactory
        {
            public IQueueClient Create(QueueOptions options)
            {
                throw new Exception();
            }
        }
    }
}
