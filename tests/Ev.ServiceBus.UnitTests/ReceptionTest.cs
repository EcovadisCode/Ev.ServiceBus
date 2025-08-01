using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Exceptions;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

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
        var client = composer.ClientFactory.GetProcessorMock("testTopic", "SubscriptionWithFailingHandler");

        var action = () => SimulateEventReception(client);

        using (new AssertionScope())
        {
           var exception = await action.Should().ThrowAsync<FailedToProcessMessageException>();
           exception.WithInnerException<ArgumentNullException>();
        }
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
        var client = composer.ClientFactory.GetProcessorMock("testTopic", "testSubscription");

        var message = new ServiceBusMessage()
        {
            ApplicationProperties = { {"wrongProperty", "wrongValue"} }
        };

        var action = () => client.TriggerMessageReception(message, CancellationToken.None);

        using (new AssertionScope())
        {
            var exception = await action.Should().ThrowAsync<FailedToProcessMessageException>();
            exception.WithInnerException<MessageIsMissingPayloadTypeIdException>();
        }
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
        var client = composer.ClientFactory.GetProcessorMock("testTopic", "SubscriptionWithNoHandlers");
        await SimulateEventReception(client);
    }

    [Fact]
    public async Task CasingDoesntMatterInPayloadTypeId()
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
                            builder.RegisterReception<SubscribedEvent, SubscribedPayloadHandler>()
                                .CustomizePayloadTypeId("MyEvent");
                        });

                services.AddSingleton(eventStore);
            });

        await composer.Compose();
        var client = composer
            .ClientFactory
            .GetProcessorMock("testTopic", "testSubscription");

        var message = TestMessageHelper.CreateEventMessage("myevent", new
        {
            SomeString = "hello",
            SomeNumber = 36
        });

        await client.TriggerMessageReception(message, CancellationToken.None);
        var @event = eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedPayloadHandler));
        Assert.NotNull(@event);
        Assert.IsType<SubscribedEvent>(@event.Event);
        Assert.Equal("hello", ((SubscribedEvent) @event.Event).SomeString);
        Assert.Equal(36, ((SubscribedEvent) @event.Event).SomeNumber);
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
        var client = composer.ClientFactory.GetProcessorMock("testTopic", "testSubscription");

        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        await SimulateEventReception(client, tokenSource.Token);
    }

    [Fact]
    public async Task MaxAutoRenewDurationIsSet()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });
        services.OverrideClientFactory();
        services.RegisterServiceBusReception()
            .FromSubscription(
                "testTopic",
                "testSubscription",
                builder =>
                {
                    builder.RegisterReception<SubscribedEvent, CancellingHandler>()
                        .CustomizePayloadTypeId("MyEvent");
                    builder.CustomizeMessageHandling(processorOptions =>
                    {
                        processorOptions.MaxConcurrentCalls = 3;
                        processorOptions.MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(20);
                    });
                });

        var provider = services.BuildServiceProvider();

        await provider.SimulateStartHost(CancellationToken.None);
        var options = provider.GetService<IOptions<ServiceBusOptions>>();
        options.Value.Receivers.Count.Should().Be(1);
        var subOptions = options.Value.Receivers.First();
        subOptions.ServiceBusProcessorOptions.Should().NotBeNull();


        var client = provider.GetProcessorMock("testTopic", "testSubscription");
        client.Options.MaxConcurrentCalls.Should().Be(3);
        client.Options.MaxAutoLockRenewalDuration.Should().Be(TimeSpan.FromMinutes(20));
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

        var client = composer
            .ClientFactory
            .GetProcessorMock("testTopic", "testSubscription");

        await SimulateEventReception(client);
        return composer;
    }

    private async Task SimulateEventReception(
        ProcessorMock client,
        CancellationToken? cancellationToken = null)
    {
        var parser = new TextJsonPayloadSerializer();
        var result = parser.SerializeBody(
            new
            {
                SomeString = "hello",
                SomeNumber = 36
            });
        var message = new ServiceBusMessage(result.Body)
        {
            ContentType = result.ContentType,
            Subject = "An integration event of type 'MyEvent'",
            ApplicationProperties =
            {
                { UserProperties.MessageTypeProperty, "IntegrationEvent" },
                { UserProperties.PayloadTypeIdProperty, "MyEvent" }
            }
        };

        // // Necessary to simulate the reception of the message
        // var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
        // if (propertyInfo != null && propertyInfo.CanWrite)
        // {
        //     propertyInfo.SetValue(message.SystemProperties, 1, null);
        // }

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