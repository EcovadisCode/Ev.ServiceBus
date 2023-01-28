using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Batching;
using Ev.ServiceBus.Batching;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class MessageBatcherTests
{
    // queue/topic tests
    // +batch composition tests
    // failing with exception tests
    // different payload types
    
    [Theory]
    [InlineData(50, 10, 5)]
    [InlineData(50, 50, 1)]
    [InlineData(50, 49, 2)]
    [InlineData(12000, 60, 200)]
    [InlineData(1, 10, 1)]
    public async Task CreatesBatches(int eventsCount, int batchSize, int expectedBatches)
    {
        var registry = new Mock<IServiceBusRegistry>();
        var dispatchRegistry = new ServiceBusRegistry();
        var resourceId = "123456789012345678901234567890"; // this should be payload type
        var sender = new Mock<IMessageSender>();
        var options = new MockClientOptions(resourceId, ClientType.Topic);
        registry.Setup(x => x.GetTopicSender(resourceId)).Returns(sender.Object);
        sender.Setup(x => x.CreateMessageBatchAsync(default)).ReturnsAsync(() => SetupMessageBatch(batchSize));
        dispatchRegistry
            .Register(
                typeof(Event),
                new[] { new MessageDispatchRegistration(options, typeof(Event)) });
        var payloadSerializer = new PayloadSerializer();

        var messageBatcher = new MessageBatcher(
            registry.Object,
            payloadSerializer,
            dispatchRegistry);

        var events = Enumerable.Range(1, eventsCount).Select(n => new Event { Payload = n.ToString() }).ToArray();
        var batches = await messageBatcher.CalculateBatches(events);

        batches.Should().HaveCount(expectedBatches);
    }

    [Fact]
    public async Task CreatesBatchesWithCorrectComposition()
    {
        var registry = new Mock<IServiceBusRegistry>();
        var dispatchRegistry = new ServiceBusRegistry();
        var resourceId = "123456789012345678901234567890"; // this should be payload type
        var sender = new Mock<IMessageSender>();
        var options = new MockClientOptions(resourceId, ClientType.Topic);
        registry.Setup(x => x.GetTopicSender(resourceId)).Returns(sender.Object);
        sender.Setup(x => x.CreateMessageBatchAsync(default)).ReturnsAsync(() => SetupMessageBatch(2));
        dispatchRegistry
            .Register(
                typeof(Event),
                new[] { new MessageDispatchRegistration(options, typeof(Event)) });
        var payloadSerializer = new PayloadSerializer();

        var messageBatcher = new MessageBatcher(
            registry.Object,
            payloadSerializer,
            dispatchRegistry);

        var events = Enumerable.Range(1, 7).Select(n => new Event { Payload = n.ToString() }).ToArray();
        var batches = await messageBatcher.CalculateBatches(events);

        batches.Should().HaveCount(4);
        batches.ElementAt(0).Should().BeEquivalentTo(
            new { Payload = "1" }, new { Payload = "2" });
        batches.ElementAt(1).Should().BeEquivalentTo(
            new { Payload = "3" }, new { Payload = "4" });
        batches.ElementAt(2).Should().BeEquivalentTo(
            new { Payload = "5" }, new { Payload = "6" });
        batches.ElementAt(3).Should().BeEquivalentTo(
            new { Payload = "7" });
    }

    private sealed class Event
    {
        public string Payload { get; set; }
    }

    private sealed class MockClientOptions : ClientOptions
    {
        public MockClientOptions(string resourceId, ClientType clientType)
            : base(resourceId, clientType, false)
        {
        }
    }

    private ServiceBusMessageBatch SetupMessageBatch(int batchSize)
    {
        const int messageSizeLimitInBytes = 256;
        var batchMessageStore = new List<ServiceBusMessage>();
        var serviceBusMessageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(
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

        return serviceBusMessageBatch;
    }
}