﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.IntegrationEvents.Subscription;
using Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests
{
    public class SubscriptionConfigurationTest
    {
        [Fact]
        public void EventTypeIdMustBeSet()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() =>
            {
                services.RegisterServiceBusReception()
                    .FromSubscription("topic", "sub",
                        builder =>
                        {
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>()
                                .CustomizeEventTypeId(null);
                        });
            });
        }

        [Fact]
        public void EventTypeIdIsAutoGenerated()
        {
            var services = new ServiceCollection();
            services.RegisterServiceBusReception()
                .FromSubscription("topic", "sub",
                    builder =>
                    {
                        var reg = builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                        reg.EventTypeId.Should().Be("SubscribedEvent");
                    });
        }

        [Fact]
        public async Task HandlerCannotReceiveFromTheSameSubscriptionTwice()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusReception()
                    .FromSubscription("topicName", "subscriptionName",
                        builder =>
                        {
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                        });
            });

            await composer.Compose();

            var exception = Assert.Throws<DuplicateSubscriptionHandlerDeclarationException>(() =>
            {
                composer.Provider.GetService(typeof(ServiceBusEventSubscriptionRegistry));
            });
            exception.Message.Should().NotBeNull();
            exception.Duplicates.Should()
                .SatisfyRespectively(ev =>
                    {
                        ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                        ev.Options.ClientType.Should().Be(ClientType.Subscription);
                        ev.EventTypeId.Should().Be("SubscribedEvent");
                    },
                    ev =>
                    {
                        ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                        ev.Options.ClientType.Should().Be(ClientType.Subscription);
                        ev.EventTypeId.Should().Be("SubscribedEvent");
                    });
        }

        [Fact]
        public async Task HandlerCannotReceiveFromTheSameQueueTwice()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusReception()
                    .FromQueue("queueName",
                        builder =>
                        {
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                        });
            });

            await composer.Compose();

            var exception = Assert.Throws<DuplicateSubscriptionHandlerDeclarationException>(() =>
            {
                composer.Provider.GetService(typeof(ServiceBusEventSubscriptionRegistry));
            });
            exception.Message.Should().NotBeNull();
            exception.Duplicates.Should()
                .SatisfyRespectively(ev =>
                    {
                        ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                        ev.Options.ClientType.Should().Be(ClientType.Queue);
                        ev.EventTypeId.Should().Be("SubscribedEvent");
                    },
                    ev =>
                    {
                        ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                        ev.Options.ClientType.Should().Be(ClientType.Queue);
                        ev.EventTypeId.Should().Be("SubscribedEvent");
                    });
        }

        [Fact]
        public async Task EventTypeIdCannotBeSetTwice()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusReception()
                    .FromQueue("queueName",
                        builder =>
                        {
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>()
                                .CustomizeEventTypeId("testEvent");
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler2>()
                                .CustomizeEventTypeId("testEvent");
                        });
            });

            await composer.Compose();

            var exception = Assert.Throws<DuplicateEvenTypeIdDeclarationException>(() =>
            {
                composer.Provider.GetService(typeof(ServiceBusEventSubscriptionRegistry));
            });
            exception.Message.Should().NotBeNull();
            exception.Duplicates.Should()
                .SatisfyRespectively(ev =>
                    {
                        ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                        ev.Options.ClientType.Should().Be(ClientType.Queue);
                        ev.EventTypeId.Should().Be("testEvent");
                    },
                    ev =>
                    {
                        ev.HandlerType.Should().Be(typeof(SubscribedEventHandler2));
                        ev.Options.ClientType.Should().Be(ClientType.Queue);
                        ev.EventTypeId.Should().Be("testEvent");
                    });
        }

        [Fact]
        public void ReceiveFromQueue_ArgumentCannotBeNull()
        {
            var services = new ServiceCollection();

            services.AddIntegrationEventHandling<BodyParser>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                services.RegisterServiceBusReception().FromQueue(null, builder => { });
            });
        }

        [Theory]
        [InlineData(null, "subscriptionName")]
        [InlineData("topicName", null)]
        public void ReceiveFromSubscription_ArgumentCannotBeNull(string topicName, string subscriptionName)
        {
            var services = new ServiceCollection();

            services.AddIntegrationEventHandling<BodyParser>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                services.RegisterServiceBusReception()
                    .FromSubscription(topicName, subscriptionName, builder => { });
            });
        }

        [Fact]
        public async Task CustomizeMessageHandling_ChangesAreAppliedToClient()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusReception()
                    .FromSubscription("testTopic", "testSubscription",
                        builder =>
                        {
                            builder.CustomizeMessageHandling(3, TimeSpan.FromDays(3));
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>()
                                .CustomizeEventTypeId("MyEvent");
                        });
            });

            await composer.Compose();

            var clients = composer
                .SubscriptionFactory
                .GetAllRegisteredClients();

            var client = clients.First(o => o.ClientName == "testSubscription");
            client.Mock.Verify(o => o.RegisterMessageHandler(It.IsAny<Func<Message, CancellationToken, Task>>(),
                It.Is<MessageHandlerOptions>(handlerOptions =>
                    handlerOptions.AutoComplete
                    && handlerOptions.MaxConcurrentCalls == 3
                    && handlerOptions.MaxAutoRenewDuration == TimeSpan.FromDays(3))), Times.Once);
        }

        [Fact]
        public async Task CustomizeConnection_ChangesAreAppliedToClient()
        {
            var composer = new Composer();

            var factory = new Mock<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            factory.Setup(o => o.Create(It.IsAny<SubscriptionOptions>(),
                    It.Is<ConnectionSettings>(settings => settings.ConnectionString == "newConnectionString"
                                                          && settings.ReceiveMode == ReceiveMode.ReceiveAndDelete)))
                .Returns(new Mock<ISubscriptionClient>().Object);
            
            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusReception()
                    .FromSubscription("testTopic", "testSubscription",
                        builder =>
                        {
                            builder.CustomizeConnection("newConnectionString", ReceiveMode.ReceiveAndDelete, RetryPolicy.NoRetry);
                            builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>()
                                .CustomizeEventTypeId("MyEvent");
                        });
                services.OverrideClientFactory(factory.Object);
            });

            await composer.Compose();
            factory.VerifyAll();
        }

        public class SubscribedEvent { }

        public class SubscribedEventHandler : IIntegrationEventHandler<SubscribedEvent>
        {
            public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        public class SubscribedEventHandler2 : IIntegrationEventHandler<SubscribedEvent>
        {
            public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
