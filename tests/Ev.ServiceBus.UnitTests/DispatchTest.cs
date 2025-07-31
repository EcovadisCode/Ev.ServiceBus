using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Extensions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Composer = Ev.ServiceBus.UnitTests.Helpers.Composer;

namespace Ev.ServiceBus.UnitTests;

public class DispatchTest : IDisposable
{
    private readonly Composer _composer;
    private readonly List<ServiceBusMessage> _sentMessagesToTopic;
    private readonly List<ServiceBusMessage> _sentMessagesToQueue;
    private readonly List<ServiceBusMessage> _sentMessagesToQueueSession;
    private readonly Mock<IMessageMetadata> _messageMetadata;

    public DispatchTest()
    {
        _sentMessagesToTopic = [];
        _sentMessagesToQueue = [];
        _sentMessagesToQueueSession = [];
        _composer = new Composer();
        _messageMetadata = new Mock<IMessageMetadata>();

        SetupComposer(_composer);
        _composer.Compose().GetAwaiter().GetResult();
        MockClients(_composer);
        SimulatePublication().GetAwaiter().GetResult();
    }

    private void SetupComposer(Composer composer)
    {
        composer.WithAdditionalServices(services =>
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

            services.RegisterServiceBusDispatch().ToQueue("testQueueSession", builder =>
            {
                builder.RegisterDispatch<PublishedThroughSessionQueueEvent>().CustomizePayloadTypeId("MyEventThroughQueue");
            });

            // noise
            services.RegisterServiceBusDispatch().ToTopic("testTopic2", builder =>
            {
                builder.RegisterDispatch<PublishedEvent2>().CustomizePayloadTypeId("MyEvent2");
            });

            var metadataAccessor = new Mock<IMessageMetadataAccessor>();

            _messageMetadata.SetupGet(x => x.CorrelationId).Returns(Guid.NewGuid().ToString);
            metadataAccessor.SetupGet(x => x.Metadata).Returns(_messageMetadata.Object);

            services.Replace(new ServiceDescriptor(typeof(IMessageMetadataAccessor), metadataAccessor.Object));
        });
    }

    private void MockClients(Composer composer)
    {
        var createMessageBatchOptions = new CreateMessageBatchOptions()
        {
            MaxSizeInBytes = 1000
        };
        var topicClient = composer.ClientFactory.GetSenderMock("testTopic");
        topicClient.Mock
            .Setup(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((ServiceBusMessage message, CancellationToken token) =>
            {
                _sentMessagesToTopic.Add(message);
            });

        topicClient.Mock.Setup(o => o.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceBusModelFactory.ServiceBusMessageBatch(0, _sentMessagesToTopic, createMessageBatchOptions));

        var queueClient = composer.ClientFactory.GetSenderMock("testQueue");
        queueClient.Mock
            .Setup(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((ServiceBusMessage message, CancellationToken token) =>
            {
                _sentMessagesToQueue.Add(message);
            });
        queueClient.Mock.Setup(o => o.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceBusModelFactory.ServiceBusMessageBatch(0, _sentMessagesToQueue, createMessageBatchOptions));

        var queueClientSession = composer.ClientFactory.GetSenderMock("testQueueSession");
        queueClientSession.Mock
            .Setup(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((ServiceBusMessage message, CancellationToken token) =>
            {
                _sentMessagesToQueueSession.Add(message);
            });
        queueClientSession.Mock.Setup(o => o.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceBusModelFactory.ServiceBusMessageBatch(0, _sentMessagesToQueueSession, createMessageBatchOptions));
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
            eventPublisher.Publish(new PublishedThroughSessionQueueEvent()
            {
                SomeNumber = 36,
                SomeString = "hello"
            }, "SomeSessionId");

            await eventDispatcher.ExecuteDispatches(CancellationToken.None);
        }
    }

    public class PublishedEvent
    {
        public string SomeString { get; set; }
        public int SomeNumber { get; set; }
    }

    public class PublishedThroughQueueEvent : PublishedEvent { }
    public class PublishedThroughSessionQueueEvent : PublishedEvent { }

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
    public void MessageMustContainTheRightPayloadTypeId(string clientToCheck, string payloadTypeId)
    {
        var message = GetMessageFrom(clientToCheck);
        Assert.True(message?.ApplicationProperties.ContainsKey("PayloadTypeId"));
        Assert.Equal(payloadTypeId, message?.ApplicationProperties["PayloadTypeId"]);
    }

    [Theory]
    [InlineData("topic")]
    [InlineData("queue")]
    [InlineData("sessionQueue")]
    public void MessageContentTypeMustBeSet(string clientToCheck)
    {
        var message = GetMessageFrom(clientToCheck);
        Assert.Equal("application/json", message?.ContentType);
    }

    [Theory]
    [InlineData("topic", typeof(PublishedEvent))]
    [InlineData("queue", typeof(PublishedThroughQueueEvent))]
    [InlineData("sessionQueue", typeof(PublishedThroughSessionQueueEvent))]
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
    [InlineData("sessionQueue")]
    public void MessageMustContainALabel(string clientToCheck)
    {
        var message = GetMessageFrom(clientToCheck);
        Assert.NotNull(message?.Subject);
    }

    [Fact]
    public void MessageMustContainASessionId()
    {
        var message = GetMessageFrom("sessionQueue");
        message?.SessionId.Should().Be("SomeSessionId");
    }

    [Theory]
    [InlineData("topic")]
    [InlineData("queue")]
    [InlineData("sessionQueue")]
    public void MessageMustContainCorrelationId(string clientToCheck)
    {
        var message = GetMessageFrom(clientToCheck);

        message?.CorrelationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldPassCorrelationIdToNewlyPublishedMessages()
    {
        var composer = new Composer();

        await composer.Compose();
        _sentMessagesToQueue.Clear();

        using var scope = _composer.Provider.CreateScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 1, SomeString = "string" });
        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 2, SomeString = "string2" });
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 3, SomeString = "string3" });
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        _sentMessagesToQueue.Should().HaveCount(3);
        _sentMessagesToQueue[0].CorrelationId.Should().NotBeEmpty();
        _sentMessagesToQueue[1].CorrelationId.Should().Be(_sentMessagesToQueue[0].CorrelationId);
        _sentMessagesToQueue[2].CorrelationId.Should().Be(_sentMessagesToQueue[0].CorrelationId);
    }

    [Fact]
    public async Task ShouldManuallySetCorrelationIdOfTheMessage()
    {
        var composer = new Composer();
        var correlationId = Guid.NewGuid().ToString();

        await composer.Compose();
        _sentMessagesToQueue.Clear();

        using var scope = _composer.Provider.CreateScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 1, SomeString = "string" },
            context => context.CorrelationId = correlationId);
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        _sentMessagesToQueue.Should().HaveCount(1);
        _sentMessagesToQueue[0].CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task ShouldManuallySetIdOfTheMessage()
    {
        var composer = new Composer();
        var messageId = Guid.NewGuid().ToString();

        await composer.Compose();
        _sentMessagesToQueue.Clear();

        using var scope = _composer.Provider.CreateScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 1, SomeString = "string" },
            context => context.MessageId = messageId);
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        _sentMessagesToQueue.Should().HaveCount(1);
        _sentMessagesToQueue[0].MessageId.Should().Be(messageId);
    }

    [Fact]
    public async Task ShouldManuallySetDiagnosticIdOfTheMessage()
    {
        var composer = new Composer();
        var diagnosticsId = DiagnosticIdHelper.GetNewId();

        await composer.Compose();
        _sentMessagesToQueue.Clear();

        using var scope = _composer.Provider.CreateScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 1, SomeString = "string" },
            context => context.DiagnosticId = diagnosticsId);
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        _sentMessagesToQueue.Should().HaveCount(1);
        _sentMessagesToQueue[0].GetDiagnosticId().Should().Be(diagnosticsId);
    }

    [Fact]
    public async Task ShouldDiagnosticIdBeEmptyIfWasNotSetOfTheMessage()
    {
        var composer = new Composer();

        await composer.Compose();
        _sentMessagesToQueue.Clear();

        using var scope = _composer.Provider.CreateScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 1, SomeString = "string" },
            context => {});
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        _sentMessagesToQueue.Should().HaveCount(1);
        _sentMessagesToQueue[0].GetDiagnosticId().Should().BeNull();
    }

    [Fact]
    public async Task ShouldDiagnosticIdBeNotEmptyIfActivityWasSet()
    {
        var composer = new Composer();

        await composer.Compose();
        _sentMessagesToQueue.Clear();

        using var scope = _composer.Provider.CreateScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();
        var activity = new Activity("test");
        activity.Start();
        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 1, SomeString = "string" },
            context => {});
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        _sentMessagesToQueue.Should().HaveCount(1);
        _sentMessagesToQueue[0].GetDiagnosticId().Should().Be(activity.Id);
        activity.Stop();
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
    public async Task SendDispatchesDoesntAcceptNulls()
    {
        var services = new ServiceCollection();
        services.AddServiceBus(settings => {});
        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var eventPublisher = scope.ServiceProvider.GetService<IDispatchSender>();
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                {
                    await eventPublisher.SendDispatches(null!);
                });
        }
    }

    [Fact]
    public async Task SendDispatch()
    {
        // configure
        var services = new ServiceCollection();
        services.AddServiceBus(settings =>
        {
            settings.WithConnection("myConnection", new ServiceBusClientOptions());
        });
        services.OverrideClientFactory();
        services.RegisterServiceBusDispatch().ToQueue("myQueue", builder =>
        {
            builder.RegisterDispatch<SubscribedEvent>();
        });
        var provider = services.BuildServiceProvider();
        await provider.SimulateStartHost(CancellationToken.None);

        // Act
        var message = new SubscribedEvent
        {
            SomeNumber = 1,
            SomeString = "Event number 1"
        };

        using (var scope = provider.CreateScope())
        {
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IDispatchSender>();
            await eventPublisher.SendDispatch(message);
        }

        // Verify
        var factory = provider.GetRequiredService<FakeClientFactory>();
        var mock = factory.GetSenderMock("myQueue");
        mock.Mock.Verify(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

        // Dispose
        await provider.SimulateStopHost(CancellationToken.None);
    }

    [Fact]
    public async Task SendDispatchesPaginateMessages()
    {
        // configure
        var services = new ServiceCollection();
        services.AddServiceBus(settings =>
        {
            settings.WithConnection("myConnection", new ServiceBusClientOptions());
        });
        services.OverrideClientFactory();
        services.RegisterServiceBusDispatch().ToQueue("myQueue", builder =>
        {
            builder.RegisterDispatch<SubscribedEvent>();
        });
        var provider = services.BuildServiceProvider();
        await provider.SimulateStartHost(new CancellationToken());

        // Act
        var messages = new SubscribedEvent[10000];
        int i = 0;
        while (i < 10000)
        {
            messages[i] = new SubscribedEvent()
            {
                SomeNumber = i + 1,
                SomeString = $"Event number {i+1}"
            };
            ++i;
        }
        using (var scope = provider.CreateScope())
        {
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IDispatchSender>();
            await eventPublisher.SendDispatches(messages);
        }

        // Verify
        var factory = provider.GetRequiredService<FakeClientFactory>();
        var mock = factory.GetSenderMock("myQueue");
        mock.Mock.Verify(o => o.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

        // Dispose
        await provider.SimulateStopHost(new CancellationToken());
    }

    [Fact]
    public async Task ScheduleDispatchesPaginateMessages()
    {
        // configure
        var services = new ServiceCollection();
        services.AddServiceBus(settings =>
        {
            settings.WithConnection("myConnection", new ServiceBusClientOptions());
        });
        services.OverrideClientFactory();
        services.RegisterServiceBusDispatch().ToQueue("myQueue", builder =>
        {
            builder.RegisterDispatch<SubscribedEvent>();
        });
        var provider = services.BuildServiceProvider();
        await provider.SimulateStartHost(new CancellationToken());

        // Act
        var messages = new SubscribedEvent[253];
        int i = 0;
        while (i < 253)
        {
            messages[i] = new SubscribedEvent()
            {
                SomeNumber = i + 1,
                SomeString = $"Event number {i+1}"
            };
            ++i;
        }

        var schedule = DateTimeOffset.UtcNow.AddDays(1);
        using (var scope = provider.CreateScope())
        {
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IDispatchSender>();
            await eventPublisher.ScheduleDispatches(messages, schedule);
        }

        // Verify
        var factory = provider.GetRequiredService<FakeClientFactory>();
        var mock = factory.GetSenderMock("myQueue");
        mock.Mock.Verify(o => o.ScheduleMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), schedule, It.IsAny<CancellationToken>()), Times.Exactly(3));

        // Dispose
        await provider.SimulateStopHost(new CancellationToken());
    }

    [Fact]
    public async Task ShouldPassOnOriginalIsolationKey()
    {
        var isolationKey = "my-isolationKey";

        _sentMessagesToQueue.Clear();
        GivenIsolationKeyInMetadata(isolationKey);

        using var scope = _composer.Provider.CreateScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 1, SomeString = "string" });
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        _sentMessagesToQueue.Should().HaveCount(1);
        _sentMessagesToQueue[0].ApplicationProperties.GetIsolationKey().Should().Be(isolationKey);
    }

    [Fact]
    public async Task ShouldAssignIsolationKeyWhenInIsolationMode()
    {
        var isolationKey = "service-isolationKey";
        var composer = new Composer();
        composer.WithDefaultSettings(settings =>
            {
                settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                settings.WithIsolation(IsolationBehavior.HandleIsolatedMessage, isolationKey);
            });
        SetupComposer(composer);
        await composer.Compose();
        MockClients(composer);
        await SimulatePublication();

        _sentMessagesToQueue.Clear();

        using var scope = composer.Provider.CreateScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        eventPublisher.Publish(new PublishedThroughQueueEvent { SomeNumber = 1, SomeString = "string" });
        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        _sentMessagesToQueue.Should().HaveCount(1);
        _sentMessagesToQueue[0].ApplicationProperties.GetIsolationKey().Should().Be(isolationKey);
    }

    private void GivenIsolationKeyInMetadata(string isolationKey)
    {
        var appProperties = new Dictionary<string, object> { { UserProperties.IsolationKey , isolationKey } };
        _messageMetadata.SetupGet(x => x.ApplicationProperties).Returns(appProperties);
    }

    private ServiceBusMessage GetMessageFrom(string clientToCheck)
    {
        if (clientToCheck == "topic")
        {
            return _sentMessagesToTopic.FirstOrDefault();
        }
        if (clientToCheck == "sessionQueue")
        {
            return _sentMessagesToQueueSession.FirstOrDefault();
        }
        return _sentMessagesToQueue.FirstOrDefault();
    }

    public void Dispose()
    {
        _composer?.Dispose();
    }
}