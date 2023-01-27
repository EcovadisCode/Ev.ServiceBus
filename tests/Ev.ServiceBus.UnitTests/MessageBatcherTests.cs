using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Batching;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class MessageBatcherTests
{
    [Fact]
    public async Task CreatesBatchUpToAllowedLimit()
    {
        const int messageSizeLimitInBytes = 100;
        var batchMessageStore = new List<ServiceBusMessage>();
        var serviceBusMessageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(
            messageSizeLimitInBytes,
            batchMessageStore,
            new CreateMessageBatchOptions { MaxSizeInBytes = messageSizeLimitInBytes },
            m => true);

        var registry = new Mock<IServiceBusRegistry>();
        var dispatchRegistry = new ServiceBusRegistry();
        var resourceId = "bob";
        var sender = new Mock<IMessageSender>();
        var options = new MockClientOptions(resourceId, ClientType.Topic);
        registry.Setup(x => x.GetTopicSender(resourceId)).Returns(sender.Object);
        sender.Setup(x => x.CreateMessageBatchAsync(default)).ReturnsAsync(serviceBusMessageBatch);
        dispatchRegistry
            .Register(
                typeof(Event),
                new[] { new MessageDispatchRegistration(options, typeof(Event)) });
        var payloadSerializer = new PayloadSerializer();

        var messageBatcher = new MessageBatcher(
            registry.Object,
            payloadSerializer,
            dispatchRegistry);

        var payload = new Event { Payload = "dsadsads" };
        var payloads = new[] { payload };

        var batches = await messageBatcher.CalculateBatches(payloads);

        batches.Should().HaveCount(2);
    }

    internal sealed class Event { public string Payload { get; set; } }

    internal sealed class MockClientOptions : ClientOptions
    {
        public MockClientOptions(string resourceId, ClientType clientType) : base(resourceId, clientType, false)
        {}
    }
}