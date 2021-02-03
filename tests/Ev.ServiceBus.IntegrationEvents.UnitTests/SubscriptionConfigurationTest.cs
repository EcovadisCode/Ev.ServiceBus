using System;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.IntegrationEvents.Subscription;
using Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests
{
    public class SubscriptionConfigurationTest
    {
        [Fact]
        public async Task EventTypeIdMustBeSet()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventSubscription<SubscribedEvent, SubscribedEventHandler>(builder => { });
            });

            await Assert.ThrowsAsync<EventTypeIdMustBeSetException>(async () => await composer.Compose());
        }

        [Fact]
        public async Task HandlerCannotReceiveFromTheSameSubscriptionTwice()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventSubscription<SubscribedEvent, SubscribedEventHandler>(builder =>
                {
                    builder.EventTypeId = "testEvent";
                    builder.ReceiveFromSubscription("topicName", "subscriptionName");
                    builder.ReceiveFromSubscription("topicName", "subscriptionName");
                });
            });

            await composer.Compose();

            var exception = Assert.Throws<DuplicateSubscriptionHandlerDeclarationException>(() =>
            {
                composer.Provider.GetService(typeof(ServiceBusEventSubscriptionRegistry));
            });
            exception.Message.Should().NotBeNull();
            exception.Duplicates.Should().SatisfyRespectively(
                ev =>
                {
                    ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                    ev.ClientType.Should().Be(ClientType.Subscription);
                    ev.EventTypeId.Should().Be("testEvent");
                },
                ev =>
                {
                    ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                    ev.ClientType.Should().Be(ClientType.Subscription);
                    ev.EventTypeId.Should().Be("testEvent");
                });
        }

        [Fact]
        public async Task HandlerCannotReceiveFromTheSameQueueTwice()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventSubscription<SubscribedEvent, SubscribedEventHandler>(builder =>
                {
                    builder.EventTypeId = "testEvent";
                    builder.ReceiveFromQueue("queueName");
                    builder.ReceiveFromQueue("queueName");
                });
            });

            await composer.Compose();

            var exception = Assert.Throws<DuplicateSubscriptionHandlerDeclarationException>(() =>
            {
                composer.Provider.GetService(typeof(ServiceBusEventSubscriptionRegistry));
            });
            exception.Message.Should().NotBeNull();
            exception.Duplicates.Should().SatisfyRespectively(
                ev =>
                {
                    ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                    ev.ClientType.Should().Be(ClientType.Queue);
                    ev.EventTypeId.Should().Be("testEvent");
                },
                ev =>
                {
                    ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                    ev.ClientType.Should().Be(ClientType.Queue);
                    ev.EventTypeId.Should().Be("testEvent");
                });
        }

        [Fact]
        public async Task EventTypeIdCannotBeSetTwice()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventSubscription<SubscribedEvent, SubscribedEventHandler>(builder =>
                {
                    builder.EventTypeId = "testEvent";
                    builder.ReceiveFromQueue("queueName");
                });
                services.RegisterIntegrationEventSubscription<SubscribedEvent, SubscribedEventHandler2>(builder =>
                {
                    builder.EventTypeId = "testEvent";
                    builder.ReceiveFromQueue("queueName");
                });
            });

            await composer.Compose();

            var exception = Assert.Throws<DuplicateEvenTypeIdDeclarationException>(() =>
            {
                composer.Provider.GetService(typeof(ServiceBusEventSubscriptionRegistry));
            });
            exception.Message.Should().NotBeNull();
            exception.Duplicates.Should().SatisfyRespectively(
                ev =>
                {
                    ev.HandlerType.Should().Be(typeof(SubscribedEventHandler));
                    ev.ClientType.Should().Be(ClientType.Queue);
                    ev.EventTypeId.Should().Be("testEvent");
                },
                ev =>
                {
                    ev.HandlerType.Should().Be(typeof(SubscribedEventHandler2));
                    ev.ClientType.Should().Be(ClientType.Queue);
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
                services.RegisterIntegrationEventSubscription<SubscribedEvent, SubscribedEventHandler>(builder =>
                {
                    builder.EventTypeId = "testEvent";
                    builder.ReceiveFromQueue(null);
                });
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
                services.RegisterIntegrationEventSubscription<SubscribedEvent, SubscribedEventHandler>(builder =>
                {
                    builder.EventTypeId = "testEvent";
                    builder.ReceiveFromSubscription(topicName, subscriptionName);
                });
            });
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
