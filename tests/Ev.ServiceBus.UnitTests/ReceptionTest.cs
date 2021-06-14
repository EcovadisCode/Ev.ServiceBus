using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class ReceptionTest
    {
        [Fact]
        public async Task TheProperEventHasBeenReceived()
        {
            var composer = await InitSimpleTest();
            var eventStore = composer.Provider.GetService<EventStore>();
            var @event = eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedPayloadHandler));
            Assert.NotNull(@event);
            Assert.IsType<SubscribedEvent>(@event.Event);
            Assert.Equal("hello", ((SubscribedEvent) @event.Event).SomeString);
            Assert.Equal(36, ((SubscribedEvent) @event.Event).SomeNumber);
        }

        [Fact]
        public async Task TheProperHandlerHasReceivedTheEvent()
        {
            var composer = await InitSimpleTest();
            var eventStore = composer.Provider.GetService<EventStore>();
            var @event = eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedPayloadHandler));
            Assert.NotNull(@event);
            Assert.Equal(typeof(SubscribedPayloadHandler), @event.HandlerType);
        }

        [Fact]
        public async Task FailsWhenHandlerThrowsException()
        {
            var composer = await InitSimpleTest();
            var clients = composer
                .SubscriptionFactory
                .GetAllRegisteredClients();

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                {
                    await SimulateEventReception(clients.First(o => o.ClientName == "SubscriptionWithFailingHandler"));
                });
        }

        [Fact]
        public async Task FailsWhenReceivedMessageHasNoPayloadTypeId()
        {
            var composer = new Composer();
            var eventStore = new EventStore();
            var logger = new Mock<ILogger<ReceiverWrapper>>();
            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(logger.Object);
                    services.RegisterServiceBusReception()
                        .FromSubscription(
                            "testTopic",
                            "testSubscription",
                            builder =>
                            {
                                builder.RegisterReception<NoiseEvent, NoiseHandler>()
                                    .CustomizePayloadTypeId("MyEvent");
                            });

                    services.AddSingleton(eventStore);
                });

            await composer.Compose();
            var clients = composer
                .SubscriptionFactory
                .GetAllRegisteredClients();
            var client = clients.First(o => o.ClientName == "testSubscription");

            var message = new Message
            {
                UserProperties = { { "wrongProperty", "wrongValue" } }
            };

            // Necessary to simulate the reception of the message
            var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(message.SystemProperties, 1, null);
            }

            await Assert.ThrowsAsync<MessageIsMissingPayloadTypeIdException>(
                async () =>
                {
                    await client.TriggerMessageReception(message, CancellationToken.None);
                });
            logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<MessageIsMissingPayloadTypeIdException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task WontFailIfMessageIsReceivedAndNoHandlersAreSet()
        {
            var composer = new Composer();
            var eventStore = new EventStore();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusReception()
                        .FromSubscription(
                            "testTopic",
                            "SubscriptionWithNoHandlers",
                            builder =>
                            {
                            });

                    services.AddSingleton(eventStore);
                });

            await composer.Compose();
            var clients = composer
                .SubscriptionFactory
                .GetAllRegisteredClients();
            var client = clients.First(o => o.ClientName == "SubscriptionWithNoHandlers");
            await SimulateEventReception(client);
        }

        [Fact]
        public async Task CancellationTokenIsPassedDownToHandlers()
        {
            var composer = new Composer();
            var eventStore = new EventStore();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusReception()
                        .FromSubscription(
                            "testTopic",
                            "testSubscription",
                            builder =>
                            {
                                builder.RegisterReception<SubscribedEvent, CancellingHandler>()
                                    .CustomizePayloadTypeId("MyEvent");
                            });

                    services.AddSingleton(eventStore);
                });

            await composer.Compose();
            var clients = composer
                .SubscriptionFactory
                .GetAllRegisteredClients();
            var client = clients.First(o => o.ClientName == "testSubscription");

            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            await SimulateEventReception(client, tokenSource.Token);
        }

        [Fact]
        public void MaxAutoRenewDurationIsSet()
        {
            var services = new ServiceCollection();

            services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                .WithConnection("testconnectionstring")
                .ToMessageReceptionHandling(3, TimeSpan.FromMinutes(20));

            var provider = services.BuildServiceProvider();

            var options = provider.GetService<IOptions<ServiceBusOptions>>();
            options.Value.Receivers.Count.Should().Be(1);
            var subOptions = options.Value.Receivers.First();
            subOptions.MessageHandlerConfig.Should().NotBeNull();

            var messageHandlerOptions = new MessageHandlerOptions(_ => Task.CompletedTask);
            subOptions.MessageHandlerConfig!(messageHandlerOptions);
            messageHandlerOptions.MaxAutoRenewDuration.Should().Be(TimeSpan.FromMinutes(20));
        }

                private async Task<Composer> InitSimpleTest()
        {
            var eventStore = new EventStore();
            var composer = new Composer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusReception()
                        .FromSubscription(
                            "testTopic",
                            "testSubscription",
                            builder =>
                            {
                                builder.RegisterReception<SubscribedEvent, SubscribedPayloadHandler>()
                                    .CustomizePayloadTypeId("MyEvent");
                            });

                    services.RegisterServiceBusReception()
                        .FromSubscription(
                            "testTopic",
                            "SubscriptionWithNoHandlers",
                            builder =>
                            {
                            });

                    services.RegisterServiceBusReception()
                        .FromSubscription(
                            "testTopic",
                            "SubscriptionWithFailingHandler",
                            builder =>
                            {
                                builder.RegisterReception<SubscribedEvent, FailingEventHandler>()
                                    .CustomizePayloadTypeId("MyEvent");
                            });

                    // noise
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription2")
                        .ToMessageReceptionHandling();

                    services.RegisterServiceBusReception()
                        .FromSubscription(
                            "testTopic2",
                            "testSubscription3",
                            builder =>
                            {
                                builder.RegisterReception<NoiseEvent, NoiseHandler>()
                                    .CustomizePayloadTypeId("MyEvent2");
                            });

                    services.AddSingleton(eventStore);
                });

            await composer.Compose();

            var clients = composer
                .SubscriptionFactory
                .GetAllRegisteredClients();

            await SimulateEventReception(clients.First(o => o.ClientName == "testSubscription"));
            return composer;
        }

        private async Task SimulateEventReception(
            SubscriptionClientMock client,
            CancellationToken? cancellationToken = null)
        {
            var parser = new PayloadSerializer();
            var result = parser.SerializeBody(
                new
                {
                    SomeString = "hello",
                    SomeNumber = 36
                });
            var message = new Message(result.Body)
            {
                ContentType = result.ContentType,
                Label = "An integration event of type 'MyEvent'",
                UserProperties =
                {
                    { UserProperties.MessageTypeProperty, "IntegrationEvent" },
                    { UserProperties.EventTypeIdProperty, "MyEvent" }
                }
            };

            // Necessary to simulate the reception of the message
            var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(message.SystemProperties, 1, null);
            }

            await client.TriggerMessageReception(message, cancellationToken ?? CancellationToken.None);
        }

        public class SubscribedPayloadHandler : StoringPayloadHandler<SubscribedEvent>
        {
            public SubscribedPayloadHandler(EventStore store) : base(store) { }
        }

        public class FailingEventHandler : IMessageReceptionHandler<SubscribedEvent>
        {
            public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
            {
                throw new ArgumentNullException();
            }
        }

        public class CancellingHandler : IMessageReceptionHandler<SubscribedEvent>
        {
            public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }

                throw new ArgumentNullException();
            }
        }
    }
}
