using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ev.ServiceBus.IntegrationEvents.Publication;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests
{
    public class PublicationConfigurationTest
    {
        [Fact]
        public async Task EventTypeIdMustBeSet()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventPublication<PublishedEvent>(config => { });
            });

            await Assert.ThrowsAsync<EventTypeIdMustBeSetException>(async () => await composer.Compose());
        }

        [Fact]
        public async Task CannotRegisterSameTopicSenderTwice()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventPublication<PublishedEvent>(config =>
                {
                    config.EventTypeId = "test";
                    config.SendToTopic("topic");
                    config.SendToTopic("topic");
                });
            });

            var exception =
                await Assert.ThrowsAsync<MultipleServiceBusPublicationRegistrationException>(
                    async () => await composer.Compose());
            exception.ClientType.Should().Be(ClientType.Topic);
            exception.TopicName.Should().Be("topic");
        }

        [Fact]
        public async Task CannotRegisterSameQueueSenderTwice()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventPublication<PublishedEvent>(config =>
                {
                    config.EventTypeId = "test";
                    config.SendToQueue("queue");
                    config.SendToQueue("queue");
                });
            });

            var exception =
                await Assert.ThrowsAsync<MultipleServiceBusPublicationRegistrationException>(
                    async () => await composer.Compose());
            exception.ClientType.Should().Be(ClientType.Queue);
            exception.TopicName.Should().Be("queue");
        }

        [Fact]
        public async Task CannotRegisterTheSameContractMoreThanOnce()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventPublication<PublishedEvent>(config =>
                {
                    config.EventTypeId = "testEvent";
                    config.SendToTopic("topic");
                });
                services.RegisterIntegrationEventPublication<PublishedEvent>(config =>
                {
                    config.EventTypeId = "testEvent2";
                    config.SendToTopic("topic");
                });
            });

            await composer.Compose();

            var exception = Assert.Throws<MultiplePublicationRegistrationException>(() =>
            {
                composer.Provider.GetService(typeof(PublicationRegistry));
            });
            exception.Registrations.Should().Contain(o => o.EventType == typeof(PublishedEvent));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(1)]
        [InlineData(5)]
        public async Task OutgoingCustomizersTests_AllCustomizersForEventWasCalled(int countOfEventsThrown)
        {
            var customizedMessage = new List<Message>();
            var customizedPayload = new List<object>();
            var composer = new Composer();

            var queueName = "queueName";
            var testEventId = "test_test_id";
            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventPublication<TestEvent>(builder =>
                {
                    builder.EventTypeId = testEventId;
                    builder.SendToQueue(queueName);
                    builder.CustomizeOutgoingMessage((a, b) =>
                    {
                        customizedMessage.Add(a);
                        customizedPayload.Add(b);
                    });
                });
            }).WithIntegrationEventsQueueSender(queueName);

            await composer.Compose();

            // Act
            var eventPublisher =
                composer.Provider.GetService(typeof(IIntegrationEventPublisher)) as IIntegrationEventPublisher;
            var eventDispatcher =
                composer.Provider.GetService(typeof(IIntegrationEventDispatcher)) as IIntegrationEventDispatcher;

            for (int i = 0; i <countOfEventsThrown; i++)
            {
                eventPublisher.Publish(new TestEvent() {EventRootId = i});
            }

            await eventDispatcher.DispatchEvents();

            // Assert
            Assert.All(customizedMessage, Assert.NotNull);
            Assert.All(customizedPayload, Assert.NotNull);
            for (int i = 0; i < customizedPayload.Count; i++)
            {
                Assert.IsType<TestEvent>(customizedPayload[i]);
                Assert.Equal(((TestEvent) customizedPayload[i]).EventRootId, i);
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(1)]
        public async Task OutgoingCustomizersTests_WhenOtherEventPublished_NoCustomizersForEventWasCalled(int countOfEventsThrown)
        {
            var customizedMessage = new List<Message>();
            var customizedPayload = new List<object>();
            var composer = new Composer();

            var queueName = "queueName";
            var testEventId = "test_test_id";
            composer.WithAdditionalServices(services =>
            {
                services.RegisterIntegrationEventPublication<TestEvent>(builder =>
                {
                    builder.EventTypeId = testEventId;
                    builder.SendToQueue(queueName);
                });

                services.RegisterIntegrationEventPublication<PublishedEvent>(builder =>
                {
                    builder.EventTypeId = testEventId;
                    builder.SendToQueue(queueName);
                    builder.CustomizeOutgoingMessage((a, b) =>
                    {
                        customizedMessage.Add(a);
                        customizedPayload.Add(b);
                    });
                });
            }).WithIntegrationEventsQueueSender(queueName);

            await composer.Compose();

            // Act
            var eventPublisher =
                composer.Provider.GetService(typeof(IIntegrationEventPublisher)) as IIntegrationEventPublisher;
            var eventDispatcher =
                composer.Provider.GetService(typeof(IIntegrationEventDispatcher)) as IIntegrationEventDispatcher;

            for (int i = 0; i <countOfEventsThrown; i++)
            {
                eventPublisher.Publish(new TestEvent() {EventRootId = i});
            }

            await eventDispatcher.DispatchEvents();

            // Assert
            Assert.Empty(customizedMessage);
            Assert.Empty(customizedPayload);
        }

        [Fact]
        public void SendToTopic_ArgumentCannotBeNull()
        {
            var services = new ServiceCollection();

            services.AddIntegrationEventHandling<BodyParser>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                services.RegisterIntegrationEventPublication<PublishedEvent>(config =>
                {
                    config.EventTypeId = "testEvent";
                    config.SendToTopic(null);
                });
            });
        }

        [Fact]
        public void SendToQueue_ArgumentCannotBeNull()
        {
            var services = new ServiceCollection();

            services.AddIntegrationEventHandling<BodyParser>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                services.RegisterIntegrationEventPublication<PublishedEvent>(config =>
                {
                    config.EventTypeId = "testEvent";
                    config.SendToQueue(null);
                });
            });
        }

        public class TestEvent
        {
            public string EventTypeId { get; set; } = "testEvent";
            public int EventRootId { get; set; }
        }

        public class PublishedEvent { }
    }
}
