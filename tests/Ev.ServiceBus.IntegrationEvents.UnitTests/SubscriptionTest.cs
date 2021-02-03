using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.IntegrationEvents.Subscription;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests
{
    public class SubscriptionTest : IDisposable
    {
        private readonly Composer _composer;
        private readonly EventStore _eventStore;

        public SubscriptionTest()
        {
            _eventStore = new EventStore();
            _composer = new Composer();

            _composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("testconnectionstring")
                        .ToIntegrationEventHandling();

                    services.RegisterServiceBusSubscription("testTopic", "SubscriptionWithNoHandlers")
                        .WithConnection("testconnectionstring")
                        .ToIntegrationEventHandling();

                    services.RegisterServiceBusSubscription("testTopic", "SubscriptionWithFailingHandler")
                        .WithConnection("testconnectionstring")
                        .ToIntegrationEventHandling();

                    // noise
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription2")
                        .WithConnection("testconnectionstring")
                        .ToIntegrationEventHandling();
                    services.RegisterServiceBusSubscription("testTopic2", "testSubscription3")
                        .WithConnection("testconnectionstring")
                        .ToIntegrationEventHandling();

                    services.RegisterIntegrationEventSubscription<SubscribedEvent, SubscribedEventHandler>(
                        builder =>
                        {
                            builder.EventTypeId = "MyEvent";
                            builder.ReceiveFromSubscription("testTopic", "testSubscription");
                        });

                    services.RegisterIntegrationEventSubscription<SubscribedEvent, FailingEventHandler>(
                        builder =>
                        {
                            builder.EventTypeId = "MyEvent";
                            builder.ReceiveFromSubscription("testTopic", "SubscriptionWithFailingHandler");
                        });

                    services.RegisterIntegrationEventSubscription<NoiseEvent, NoiseHandler>(
                        builder =>
                        {
                            builder.EventTypeId = "MyEvent2";
                            builder.ReceiveFromSubscription("testTopic2", "testSubscription3");
                        });

                    services.AddSingleton(_eventStore);
                });

            _composer.Compose().GetAwaiter().GetResult();

            var clients = _composer
                .SubscriptionFactory
                .GetAllRegisteredClients();

            SimulateEventReception(clients.First(o => o.ClientName == "testSubscription")).GetAwaiter().GetResult();
            SimulateEventReception(clients.First(o => o.ClientName == "SubscriptionWithFailingHandler"))
                .GetAwaiter()
                .GetResult();
        }

        public void Dispose()
        {
            _composer?.Dispose();
        }

        [Fact]
        public void TheProperEventHasBeenReceived()
        {
            var @event = _eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedEventHandler));
            Assert.NotNull(@event);
            Assert.IsType<SubscribedEvent>(@event.Event);
            Assert.Equal("hello", ((SubscribedEvent) @event.Event).SomeString);
            Assert.Equal(36, ((SubscribedEvent) @event.Event).SomeNumber);
        }

        [Fact]
        public void TheProperHandlerHasReceivedTheEvent()
        {
            var @event = _eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedEventHandler));
            Assert.NotNull(@event);
            Assert.Equal(typeof(SubscribedEventHandler), @event.HandlerType);
        }


        [Fact]
        public async Task ThrowsWhenReceivedMessageHasNoEventTypeId()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("testconnectionstring")
                        .ToIntegrationEventHandling();

                    services.RegisterIntegrationEventSubscription<NoiseEvent, NoiseHandler>(
                        builder =>
                        {
                            builder.EventTypeId = "MyEvent";
                            builder.ReceiveFromSubscription("testTopic", "testSubscription");
                        });

                    services.AddSingleton(_eventStore);
                });

            await composer.Compose();
            var clients = composer
                .SubscriptionFactory
                .GetAllRegisteredClients();
            var client = clients.First(o => o.ClientName == "testSubscription");

            var message = new Message()
            {
                UserProperties = { {"wrongProperty", "wrongValue"} }
            };

            // Necessary to simulate the reception of the message
            var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(message.SystemProperties, 1, null);
            }

            var exception = await Assert.ThrowsAsync<MessageIsMissingEventTypeIdException>(async () =>
            {
                await client.TriggerMessageReception(message, CancellationToken.None);
            });
            exception.Message.Should().NotBeNull();
        }

        [Fact]
        public async Task WontFailIfMessageIsReceivedAndNoHandlersAreSet()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusSubscription("testTopic", "SubscriptionWithNoHandlers")
                        .WithConnection("testconnectionstring")
                        .ToIntegrationEventHandling();

                    services.AddSingleton(_eventStore);
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

            composer.WithAdditionalServices(
                services =>
                {
                    services.RegisterServiceBusSubscription("testTopic", "testSubscription")
                        .WithConnection("testconnectionstring")
                        .ToIntegrationEventHandling();

                    services.RegisterIntegrationEventSubscription<SubscribedEvent, CancellingHandler>(
                        builder =>
                        {
                            builder.EventTypeId = "MyEvent";
                            builder.ReceiveFromSubscription("testTopic", "testSubscription");
                        });

                    services.AddSingleton(_eventStore);
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
                .ToIntegrationEventHandling(3, TimeSpan.FromMinutes(20));

            var provider = services.BuildServiceProvider();

            var options = provider.GetService<IOptions<ServiceBusOptions>>();
            options.Value.Subscriptions.Count.Should().Be(1);
            var subOptions = options.Value.Subscriptions.First();
            subOptions.MessageHandlerConfig.Should().NotBeNull();

            var messageHandlerOptions = new MessageHandlerOptions(_ => Task.CompletedTask);
            subOptions.MessageHandlerConfig!(messageHandlerOptions);
            messageHandlerOptions.MaxAutoRenewDuration.Should().Be(TimeSpan.FromMinutes(20));
        }

        private async Task SimulateEventReception(
            SubscriptionClientMock client,
            CancellationToken? cancellationToken = null)
        {
            var parser = new BodyParser();
            var result = parser.SerializeBody(
                new
                {
                    SomeString = "hello", SomeNumber = 36
                });
            var message = new Message(result.Body)
            {
                ContentType = result.ContentType,
                Label = $"An integration event of type 'MyEvent'",
                UserProperties =
                {
                    {UserProperties.MessageTypeProperty, "IntegrationEvent"},
                    {UserProperties.EventTypeIdProperty, "MyEvent"}
                },
            };

            // Necessary to simulate the reception of the message
            var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(message.SystemProperties, 1, null);
            }

            await client.TriggerMessageReception(message, cancellationToken ?? CancellationToken.None);
        }

        public class SubscribedEventHandler : StoringEventHandler<SubscribedEvent>
        {
            public SubscribedEventHandler(EventStore store) : base(store) { }
        }

        public class FailingEventHandler : IIntegrationEventHandler<SubscribedEvent>
        {
            public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
            {
                throw new ArgumentNullException();
            }
        }

        public class NoiseHandler : StoringEventHandler<NoiseEvent>
        {
            public NoiseHandler(EventStore store) : base(store) { }
        }

        public class CancellingHandler : IIntegrationEventHandler<SubscribedEvent>
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
