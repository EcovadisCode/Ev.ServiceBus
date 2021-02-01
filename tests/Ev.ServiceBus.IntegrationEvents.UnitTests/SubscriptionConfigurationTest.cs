using System;
using System.Threading;
using System.Threading.Tasks;
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
            exception.Types.Should().Contain(typeof(SubscribedEventHandler));
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
            exception.Types.Should().Contain(typeof(SubscribedEventHandler));
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

    }
}
