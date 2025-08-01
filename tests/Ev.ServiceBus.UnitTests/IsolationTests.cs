using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class IsolationTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "Test.Application")]
    [InlineData("AnotherIsolationKey", null)]
    [InlineData("AnotherIsolationKey", "Test.Application")]
    [InlineData("AnotherIsolationKey", "Another.Application,Test.Application")]
    [InlineData("MyIsolationKey", "Another.Application")]
    [InlineData("MyIsolationKey", null)]
    public async Task When_HandleIsolatedMessage_DoesntReceiveMessagesFromDifferentInstance(string? givenIsolationKey, string? givenIsolationApps)
    {
        var composer = new Composer();
        var eventStore = new EventStore();
        composer.WithDefaultSettings(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            settings.WithIsolation(IsolationBehavior.HandleIsolatedMessage, "MyIsolationKey", "Test.Application");
        });

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
        message.ApplicationProperties[UserProperties.IsolationKey] = givenIsolationKey;
        message.ApplicationProperties[UserProperties.IsolationApps] = givenIsolationApps;

        await client.TriggerMessageReception(message, CancellationToken.None);
        var @event = eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedPayloadHandler));
        @event.Should().BeNull();

        var messageSent = composer.ClientFactory.GetSenderMock("testTopic").MessagesSent.SingleOrDefault();
        messageSent.Should().NotBeNull();
        messageSent!.ApplicationProperties[UserProperties.IsolationKey].Should().Be(givenIsolationKey);
        messageSent.ApplicationProperties[UserProperties.IsolationApps].Should().Be(givenIsolationApps);
        messageSent.ApplicationProperties[UserProperties.PayloadTypeIdProperty].Should().Be("myevent");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "Test.Application")]
    [InlineData("AnotherIsolationKey", null)]
    [InlineData("AnotherIsolationKey", "Test.Application")]
    [InlineData("AnotherIsolationKey", "Another.Application,Test.Application")]
    [InlineData("MyIsolationKey", "Another.Application")]
    [InlineData("MyIsolationKey", null)]
    public async Task When_HandleIsolatedMessage_DoesntReceiveMessagesFromDifferentInstance_ForQueues(string? givenIsolationKey, string? givenIsolationApps)
    {
        var composer = new Composer();
        var eventStore = new EventStore();
        composer.WithDefaultSettings(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            settings.WithIsolation(IsolationBehavior.HandleIsolatedMessage, "MyIsolationKey", "Test.Application");
        });

        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusReception()
                    .FromQueue(
                        "testQueue",
                        builder =>
                        {
                            builder.RegisterReception<SubscribedEvent, SubscribedPayloadHandler>()
                                .CustomizePayloadTypeId("MyEvent");
                        });

                services.AddSingleton(eventStore);
            });

        await composer.Compose();
        var client = composer.ClientFactory.GetProcessorMock("testQueue");

        var message = TestMessageHelper.CreateEventMessage("myevent", new
        {
            SomeString = "hello",
            SomeNumber = 36
        });
        message.ApplicationProperties[UserProperties.IsolationKey] = givenIsolationKey;
        message.ApplicationProperties[UserProperties.IsolationApps] = givenIsolationApps;

        await client.TriggerMessageReception(message, CancellationToken.None);
        var @event = eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedPayloadHandler));
        @event.Should().BeNull();

        var messageSent = composer.ClientFactory.GetSenderMock("testQueue").MessagesSent.SingleOrDefault();
        messageSent.Should().NotBeNull();
        messageSent!.ApplicationProperties[UserProperties.IsolationKey].Should().Be(givenIsolationKey);
        messageSent.ApplicationProperties[UserProperties.IsolationApps].Should().Be(givenIsolationApps);
        messageSent.ApplicationProperties[UserProperties.PayloadTypeIdProperty].Should().Be("myevent");
    }

    [Theory]
    [InlineData("MyIsolationKey", "Test.Application")]
    [InlineData("MyIsolationKey", "Test.Application,Another.Application")]
    public async Task When_HandleIsolatedMessage_ReceiveMessagesIsolatedMessages(string? givenIsolationKey, string? givenIsolationApps)
    {
        var composer = new Composer();
        var eventStore = new EventStore();
        composer.WithDefaultSettings(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            settings.WithIsolation(IsolationBehavior.HandleIsolatedMessage, "MyIsolationKey", "Test.Application");
        });

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
        message.ApplicationProperties[UserProperties.IsolationKey] = givenIsolationKey;
        message.ApplicationProperties[UserProperties.IsolationApps] = givenIsolationApps;

        await client.TriggerMessageReception(message, CancellationToken.None);
        var @event = eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedPayloadHandler));
        @event.Should().NotBeNull();
    }

    [Theory]
    [InlineData("MyIsolationKey", "Test.Application")]
    [InlineData("MyIsolationKey", "Test.Application,Another.Application")]
    public async Task When_HandleIsolatedMessage_ReceiveMessagesIsolatedMessages_ForQueues(string? givenIsolationKey, string? givenIsolationApps)
    {
        var composer = new Composer();
        var eventStore = new EventStore();
        composer.WithDefaultSettings(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            settings.WithIsolation(IsolationBehavior.HandleIsolatedMessage, "MyIsolationKey", "Test.Application");
        });

        composer.WithAdditionalServices(
            services =>
            {
                services.RegisterServiceBusReception()
                    .FromQueue(
                        "testQueue",
                        builder =>
                        {
                            builder.RegisterReception<SubscribedEvent, SubscribedPayloadHandler>()
                                .CustomizePayloadTypeId("MyEvent");
                        });

                services.AddSingleton(eventStore);
            });

        await composer.Compose();
        var client = composer.ClientFactory.GetProcessorMock("testQueue");

        var message = TestMessageHelper.CreateEventMessage("myevent", new
        {
            SomeString = "hello",
            SomeNumber = 36
        });
        message.ApplicationProperties[UserProperties.IsolationKey] = givenIsolationKey;
        message.ApplicationProperties[UserProperties.IsolationApps] = givenIsolationApps;

        await client.TriggerMessageReception(message, CancellationToken.None);
        var @event = eventStore.Events.FirstOrDefault(o => o.HandlerType == typeof(SubscribedPayloadHandler));
        @event.Should().NotBeNull();
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
