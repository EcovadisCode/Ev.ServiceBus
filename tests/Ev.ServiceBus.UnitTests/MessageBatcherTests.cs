using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Exceptions;
using Ev.ServiceBus.Batching;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class MessageBatcherTests
{
    private readonly Mock<IServiceBusRegistry> _registry;
    private readonly Mock<IMessageSender> _sender;
    private readonly ServiceBusRegistry _dispatchRegistry;
    private readonly MessageBatcher _messageBatcher;

    public MessageBatcherTests()
    {
        _registry = new Mock<IServiceBusRegistry>();
        _sender = new Mock<IMessageSender>();
        _dispatchRegistry = new ServiceBusRegistry();

        _messageBatcher = new MessageBatcher(
            _registry.Object,
            new PayloadSerializer(),
            _dispatchRegistry);
    }

    [Theory]
    [InlineData(50, 10, 5)]
    [InlineData(50, 50, 1)]
    [InlineData(50, 49, 2)]
    [InlineData(12000, 60, 200)]
    [InlineData(1, 10, 1)]
    public async Task CreatesBatches(int eventsCount, int batchSize, int expectedBatches)
    {
        SetupTopic<Event>();
        _sender
            .Setup(x => x.CreateMessageBatchAsync(default))
            .ReturnsAsync(() => CreateServiceBusMessageBatch(batchSize));
        var events = CreateEvents(eventsCount, p => new Event { Payload = p });

        var batches = await _messageBatcher.CalculateBatches(events);

        batches.Should().HaveCount(expectedBatches);
    }

    [Fact]
    public async Task CreatesBatchesWithCorrectComposition()
    {
        SetupTopic<Event>();
        _sender
            .Setup(x => x.CreateMessageBatchAsync(default))
            .ReturnsAsync(() => CreateServiceBusMessageBatch(2));
        var events = CreateEvents(7, p => new Event { Payload = p });

        var batches = await _messageBatcher.CalculateBatches(events);

        batches.Should().HaveCount(4);
        batches.ElementAt(0).Should().BeEquivalentTo(new { Payload = "1" }, new { Payload = "2" });
        batches.ElementAt(1).Should().BeEquivalentTo(new { Payload = "3" }, new { Payload = "4" });
        batches.ElementAt(2).Should().BeEquivalentTo(new { Payload = "5" }, new { Payload = "6" });
        batches.ElementAt(3).Should().BeEquivalentTo(new { Payload = "7" });
    }

    [Fact]
    public async Task CreatesBatchesWithDifferentTypes()
    {
        SetupTopic<Event>();
        SetupTopic<Event2>();
        _sender
            .Setup(x => x.CreateMessageBatchAsync(default))
            .ReturnsAsync(() => CreateServiceBusMessageBatch(2));
        var events1 = CreateEvents(5, p => new Event { Payload = p });
        var events2 = CreateEvents(3, p => new Event2 { Content = p });
        var events = events1
            .OfType<object>()
            .Concat(events2)
            .ToArray();

        var batches = await _messageBatcher.CalculateBatches(events);

        batches.Should().HaveCount(5);
        batches.ElementAt(0).Should().BeEquivalentTo(new { Payload = "1" }, new { Payload = "2" });
        batches.ElementAt(1).Should().BeEquivalentTo(new { Payload = "3" }, new { Payload = "4" });
        batches.ElementAt(2).Should().BeEquivalentTo(new { Payload = "5" });
        batches.ElementAt(3).Should().BeEquivalentTo(new { Content = "1" }, new { Content = "2" });
        batches.ElementAt(4).Should().BeEquivalentTo(new { Content = "3" });
    }

    [Fact]
    public async Task CreatesBatchesWithBothTopicAndQueue()
    {
        SetupTopic<Event>();
        SetupQueue<Event2>();
        _sender
            .Setup(x => x.CreateMessageBatchAsync(default))
            .ReturnsAsync(() => CreateServiceBusMessageBatch(2));
        var events1 = CreateEvents(5, p => new Event { Payload = p });
        var events2 = CreateEvents(3, p => new Event2 { Content = p });
        var events = events1
            .OfType<object>()
            .Concat(events2)
            .ToArray();

        var batches = await _messageBatcher.CalculateBatches(events);

        batches.Should().HaveCount(5);
        batches.ElementAt(0).Should().BeEquivalentTo(new { Payload = "1" }, new { Payload = "2" });
        batches.ElementAt(1).Should().BeEquivalentTo(new { Payload = "3" }, new { Payload = "4" });
        batches.ElementAt(2).Should().BeEquivalentTo(new { Payload = "5" });
        batches.ElementAt(3).Should().BeEquivalentTo(new { Content = "1" }, new { Content = "2" });
        batches.ElementAt(4).Should().BeEquivalentTo(new { Content = "3" });
    }

    [Fact]
    public void ThrowsBatchingFailedException_When_AddingMessageToBatch_IsUnsuccessful()
    {
        SetupTopic<Event>();
        _sender
            .SetupSequence(x => x.CreateMessageBatchAsync(default))
            .ReturnsAsync(CreateServiceBusMessageBatch(5))
            .ReturnsAsync(CreateServiceBusMessageBatchWhichFailsToAddMessage());
        var events = CreateEvents(7, p => new Event { Payload = p });

        var act = async () => await _messageBatcher.CalculateBatches(events);

        act.Should().Throw<BatchingFailedException>();
    }

    private sealed class Event
    {
        public string Payload { get; set; }
    }

    private sealed class Event2
    {
        public string Content { get; set; }
    }

    private sealed class MockClientOptions : ClientOptions
    {
        public MockClientOptions(string resourceId, ClientType clientType)
            : base(resourceId, clientType, false)
        {
        }
    }

    private void SetupTopic<T>()
    {
        var resourceId = typeof(T).FullName;
        var options = new MockClientOptions(resourceId, ClientType.Topic);
        _registry
            .Setup(x => x.GetTopicSender(resourceId))
            .Returns(_sender.Object);
        _dispatchRegistry
            .Register(typeof(T), new[] { new MessageDispatchRegistration(options, typeof(T)) });
    }

    private void SetupQueue<T>()
    {
        var resourceId = typeof(T).FullName;
        var options = new MockClientOptions(resourceId, ClientType.Queue);
        _registry
            .Setup(x => x.GetQueueSender(resourceId))
            .Returns(_sender.Object);
        _dispatchRegistry
            .Register(typeof(T), new[] { new MessageDispatchRegistration(options, typeof(T)) });
    }

    private static T[] CreateEvents<T>(int count, Func<string, T> factory) =>
        Enumerable
            .Range(1, count)
            .Select(n => factory(n.ToString()))
            .ToArray();

    private static ServiceBusMessageBatch CreateServiceBusMessageBatchWhichFailsToAddMessage()
    {
        const int messageSizeLimitInBytes = 256;

        return ServiceBusModelFactory.ServiceBusMessageBatch(
            messageSizeLimitInBytes,
            new List<ServiceBusMessage>(),
            new CreateMessageBatchOptions { MaxSizeInBytes = messageSizeLimitInBytes },
            _ => false);
    }

    private static ServiceBusMessageBatch CreateServiceBusMessageBatch(int batchSize)
    {
        const int messageSizeLimitInBytes = 256;
        var batchMessageStore = new List<ServiceBusMessage>();

        return ServiceBusModelFactory.ServiceBusMessageBatch(
            messageSizeLimitInBytes,
            batchMessageStore,
            new CreateMessageBatchOptions { MaxSizeInBytes = messageSizeLimitInBytes },
            _ =>
            {
                var isBatchIncomplete = batchMessageStore.Count < batchSize;
                if (isBatchIncomplete == false)
                {
                    batchMessageStore.Clear();
                }

                return isBatchIncomplete;
            });
    }
}