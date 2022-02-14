using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class DispatchTest : IDisposable
{
    private readonly Composer _composer;
    private readonly List<ServiceBusMessage> _sentMessagesToTopic;
    private readonly List<ServiceBusMessage> _sentMessagesToQueue;

    public DispatchTest()
    {
        _sentMessagesToTopic = new List<ServiceBusMessage>();
        _sentMessagesToQueue = new List<ServiceBusMessage>();
        _composer = new Composer();

        _composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToTopic("testTopic", builder =>
            {
                builder.RegisterDispatch<PublishedEvent>().CustomizePayloadTypeId("MyEvent");

                // noise
                builder.RegisterDispatch<PublishedEvent3>().CustomizePayloadTypeId("MyEvent3");
            });
            services.RegisterServiceBusDispatch().ToQueue("testQueue", builder =>
            {
                builder.RegisterDispatch<PublishedThroughQueueEvent>().CustomizePayloadTypeId("MyEventThroughQueue");
            });

            // noise
            services.RegisterServiceBusDispatch().ToTopic("testTopic2", builder =>
            {
                builder.RegisterDispatch<PublishedEvent2>().CustomizePayloadTypeId("MyEvent2");
            });
        });

        _composer.Compose().GetAwaiter().GetResult();

        var topicClient = _composer.ClientFactory.GetSenderMock("testTopic");
        topicClient.Mock
            .Setup(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((ServiceBusMessage message, CancellationToken token) =>
            {
                _sentMessagesToTopic.Add(message);
            });

        topicClient.Mock
            .Setup(o => o.SendMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((IEnumerable<ServiceBusMessage> messages, CancellationToken token) =>
            {
                _sentMessagesToTopic.AddRange(messages);
            });

        var queueClient = _composer.ClientFactory.GetSenderMock("testQueue");
        queueClient.Mock
            .Setup(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((ServiceBusMessage message, CancellationToken token) =>
            {
                _sentMessagesToQueue.Add(message);
            });

        queueClient.Mock
            .Setup(o => o.SendMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((IEnumerable<ServiceBusMessage> messages, CancellationToken token) =>
            {
                _sentMessagesToQueue.AddRange(messages);
            });

        SimulatePublication().GetAwaiter().GetResult();
    }

    private async Task SimulatePublication()
    {
        using (var scope = _composer.Provider.CreateScope())
        {
            var eventPublisher = scope.ServiceProvider.GetService<IMessagePublisher>();
            var eventDispatcher = scope.ServiceProvider.GetService<IMessageDispatcher>();

            eventPublisher.Publish(new PublishedEvent()
            {
                SomeNumber = 36,
                SomeString = "hello"
            });
            eventPublisher.Publish(new PublishedThroughQueueEvent()
            {
                SomeNumber = 36,
                SomeString = "hello"
            });

            await eventDispatcher.ExecuteDispatches();
        }
    }

    public class PublishedEvent
    {
        public string SomeString { get; set; }
        public int SomeNumber { get; set; }
    }

    public class PublishedThroughQueueEvent : PublishedEvent { }

    public class PublishedEvent2 { }
    public class PublishedEvent3 { }

    [Theory]
    [InlineData("topic")]
    [InlineData("queue")]
    public void MessageMustBeSentToTheConfiguredTopic(string clientToCheck)
    {
        Assert.NotNull(GetMessageFrom(clientToCheck));
    }

    [Theory]
    [InlineData("topic")]
    [InlineData("queue")]
    public void MessageMustContainTheRightMessageType(string clientToCheck)
    {
        var message = GetMessageFrom(clientToCheck);
        Assert.True(message?.ApplicationProperties.ContainsKey("MessageType"));
        Assert.Equal("IntegrationEvent", message?.ApplicationProperties["MessageType"]);
    }

    [Theory]
    [InlineData("topic", "MyEvent")]
    [InlineData("queue", "MyEventThroughQueue")]
    public void MessageMustContainTheRightPayloadTypeId(string clientToCheck, string eventTypeId)
    {
        var message = GetMessageFrom(clientToCheck);
        Assert.True(message?.ApplicationProperties.ContainsKey("EventTypeId"));
        Assert.True(message?.ApplicationProperties.ContainsKey("PayloadTypeId"));
        Assert.Equal(eventTypeId, message?.ApplicationProperties["EventTypeId"]);
        Assert.Equal(eventTypeId, message?.ApplicationProperties["PayloadTypeId"]);
    }

    [Theory]
    [InlineData("topic")]
    [InlineData("queue")]
    public void MessageContentTypeMustBeSet(string clientToCheck)
    {
        var message = GetMessageFrom(clientToCheck);
        Assert.Equal("application/json", message?.ContentType);
    }

    [Theory]
    [InlineData("topic", typeof(PublishedEvent))]
    [InlineData("queue", typeof(PublishedThroughQueueEvent))]
    public void MessageMustContainAProperJsonBody(string clientToCheck, Type typeToParse)
    {
        var message = GetMessageFrom(clientToCheck);
        var body = Encoding.UTF8.GetString(message?.Body.ToArray());
        var @event = JsonSerializer.Deserialize(body, typeToParse) as PublishedEvent;
        Assert.NotNull(@event);
        Assert.Equal("hello", @event.SomeString);
        Assert.Equal(36, @event.SomeNumber);
    }

    [Theory]
    [InlineData("topic")]
    [InlineData("queue")]
    public void MessageMustContainALabel(string clientToCheck)
    {
        var message = GetMessageFrom(clientToCheck);
        Assert.NotNull(message?.Subject);
    }

    [Fact]
    public async Task PublishDoesntAcceptNulls()
    {
        var composer = new Composer();
        await composer.Compose();
        using (var scope = _composer.Provider.CreateScope())
        {
            var eventPublisher = scope.ServiceProvider.GetService<IMessagePublisher>();
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    eventPublisher.Publish<SubscribedEvent>(null);
                });
        }
    }

    [Fact]
    public void SendEventsDoesntAcceptNulls()
    {
        var services = new ServiceCollection();
        services.AddServiceBus<PayloadSerializer>(settings => {});
        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var eventPublisher = scope.ServiceProvider.GetService<IDispatchSender>();
            Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                {
                    await eventPublisher.SendDispatches(null!);
                });
        }
    }

    private ServiceBusMessage GetMessageFrom(string clientToCheck)
    {
        if (clientToCheck == "topic")
        {
            return _sentMessagesToTopic.FirstOrDefault();
        }
        return _sentMessagesToQueue.FirstOrDefault();
    }

    public void Dispose()
    {
        _composer?.Dispose();
    }
}