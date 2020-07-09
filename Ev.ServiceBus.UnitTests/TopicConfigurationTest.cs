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
    public class TopicConfigurationTest
    {
        [Fact]
        public async Task CannotRegisterTwoTopicsWithTheSameName()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options.RegisterTopic("testTopic");
                    options.RegisterTopic("testTopic");
                });
            });

            await Assert.ThrowsAnyAsync<DuplicateTopicRegistrationException>(async () => await composer.ComposeAndSimulateStartup());
        }

        [Fact]
        public async Task CanRegisterAndRetrieveTopics()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.ConfigureServiceBus(options =>
                {
                    options.RegisterTopic("testTopic").WithConnectionString("testConnectionString");
                    options.RegisterTopic("testTopic2").WithConnectionString("testConnectionString2");
                    options.RegisterTopic("testTopic3").WithConnectionString("testConnectionString3");
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
            Assert.Equal("testTopic2", registry.GetTopicSender("testTopic2")?.Name);
            Assert.Equal("testTopic3", registry.GetTopicSender("testTopic3")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithConnection()
        {
            var composer = new ServiceBusComposer();

            var serviceBusConnection = new ServiceBusConnection(
                "Endpoint=sb://labepdvsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TOEhvlmrOoLjHfxhYJ3xjoLtVZrMQLqP8MUwrv5flOA=");
            var factory = new Mock<ITopicClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<TopicOptions>(opts => opts.Connection == serviceBusConnection)))
                .Returns((TopicOptions o) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideTopicClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterTopic("testTopic").WithConnection(serviceBusConnection);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithConnectionString()
        {
            var composer = new ServiceBusComposer();

            var factory = new Mock<ITopicClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<TopicOptions>(opts => opts.ConnectionString == "testConnectionString")))
                .Returns((TopicOptions o) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideTopicClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterTopic("testTopic").WithConnectionString("testConnectionString");
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithConnectionStringBuilder()
        {
            var composer = new ServiceBusComposer();

            var connectionStringBuilder = new ServiceBusConnectionStringBuilder();
            var factory = new Mock<ITopicClientFactory>();
            factory
                .Setup(
                    o => o.Create(It.Is<TopicOptions>(opts => opts.ConnectionStringBuilder == connectionStringBuilder)))
                .Returns((TopicOptions o) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideTopicClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterTopic("testTopic").WithConnectionStringBuilder(connectionStringBuilder);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithReceiveMode()
        {
            var composer = new ServiceBusComposer();

            var factory = new Mock<ITopicClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<TopicOptions>(opts => opts.ReceiveMode == ReceiveMode.ReceiveAndDelete)))
                .Returns((TopicOptions o) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideTopicClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterTopic("testTopic")
                                .WithConnectionString("testConnectionString")
                                .WithReceiveMode(ReceiveMode.ReceiveAndDelete);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
        }

        [Fact]
        public async Task CanRegisterTopicWithRetryPolicy()
        {
            var composer = new ServiceBusComposer();

            var retryPolicy = new NoRetry();
            var factory = new Mock<ITopicClientFactory>();
            factory
                .Setup(o => o.Create(It.Is<TopicOptions>(opts => opts.RetryPolicy == retryPolicy)))
                .Returns((TopicOptions o) => new TopicClientMock("testTopic").Client)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.OverrideTopicClientFactory(factory.Object);

                    services.ConfigureServiceBus(
                        options =>
                        {
                            options.RegisterTopic("testTopic")
                                .WithConnectionString("testConnectionString")
                                .WithRetryPolicy(retryPolicy);
                        });
                });

            var provider = await composer.ComposeAndSimulateStartup();

            var registry = provider.GetService<IServiceBusRegistry>();

            factory.VerifyAll();
            Assert.Equal("testTopic", registry.GetTopicSender("testTopic")?.Name);
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
                    options.RegisterTopic("testTopic").WithConnectionString("testConnectionString");
                });

            var provider = services.BuildServiceProvider();
            await provider.SimulateStartHost(token: new CancellationToken());
            var composer = new ServiceBusComposer();
            composer.OverrideQueueClientFactory(new QueueConfigurationTest.FailingQueueClientFactory());

            var registry = provider.GetService<ServiceBusRegistry>();
            await registry.GetTopicSender("testTopic").SendAsync(new Message());
        }
    }
}
