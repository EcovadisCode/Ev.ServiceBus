using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class MessageMetadataTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("CustomPayloadTypeIdProperty")]
    public async Task MetadataIsProperlySet(string customPropertyName)
    {
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.AddSingleton<List<IMessageMetadata>>();
                services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
                {
                    builder.RegisterReception<Payload, MetadataHandler>();
                    builder.WithCustomPayloadTypeIdProperty(customPropertyName);
                });
            });

        var provider = await composer.Compose();

        var clientMock = provider.GetProcessorMock("testQueue");

        await SimulateEventReception(clientMock, customPayloadTypeIdPropertyName: customPropertyName);

        var metadatas = provider.GetRequiredService<List<IMessageMetadata>>();
        metadatas.Count.Should().Be(1);

        var metadata = metadatas[0];
        metadata.Subject.Should().Be("An integration event of type 'MyEvent'");
        metadata.ContentType.Should().Be("application/json");
        metadata.ApplicationProperties.Keys.Should()
            .Contain(UserProperties.MessageTypeProperty)
            .And
            .Contain(customPropertyName ?? UserProperties.DefaultPayloadTypeIdProperty);
        metadata.CorrelationId.Should().Be("8B4C4C3C-482A-4688-8458-AFF9998C0A12");
        metadata.SessionId.Should().Be("ABB8761B-C22E-407E-801C-DFAF68916F04");
    }

    [Fact]
    public async Task MessageManagementMethodsAreCalled()
    {
        var receiver = new Mock<ServiceBusReceiver>();
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.AddSingleton<List<IMessageMetadata>>();
                services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
                {
                    builder.RegisterReception<Payload, MetadataHandler>();
                });
            });

        var provider = await composer.Compose();

        var clientMock = provider.GetProcessorMock("testQueue");

        await SimulateEventReception(clientMock, null, receiver.Object);

        var metadatas = provider.GetRequiredService<List<IMessageMetadata>>();
        metadatas.Count.Should().Be(1);

        receiver.Verify(o => o.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()));
        receiver.Verify(o => o.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()));
        receiver.Verify(o => o.DeferMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()));
        receiver.Verify(o => o.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task MessageManagementMethodsAreCalledForSession()
    {
        var receiver = new Mock<ServiceBusSessionReceiver>();
        var composer = new Composer();

        composer.WithAdditionalServices(
            services =>
            {
                services.AddSingleton<List<IMessageMetadata>>();
                services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
                {
                    builder.EnableSessionHandling(options => { });
                    builder.RegisterReception<Payload, MetadataHandler>();
                });
            });

        var provider = await composer.Compose();

        var clientMock = provider.GetSessionProcessorMock("testQueue");

        await SimulateSessionEventReception(clientMock, null, receiver.Object);

        var metadatas = provider.GetRequiredService<List<IMessageMetadata>>();
        metadatas.Count.Should().Be(1);

        receiver.Verify(o => o.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()));
        receiver.Verify(o => o.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()));
        receiver.Verify(o => o.DeferMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()));
        receiver.Verify(o => o.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()));
    }

    private async Task SimulateEventReception(
        ProcessorMock client,
        CancellationToken? cancellationToken = null,
        ServiceBusReceiver receiver = null,
        string customPayloadTypeIdPropertyName = null)
    {
        var parser = new TextJsonPayloadSerializer();
        var result = parser.SerializeBody(new { });
        var message = new ServiceBusMessage(result.Body)
        {
            ContentType = result.ContentType,
            Subject = "An integration event of type 'MyEvent'",
            ApplicationProperties =
            {
                { UserProperties.MessageTypeProperty, "IntegrationEvent" },
                { customPayloadTypeIdPropertyName ?? UserProperties.DefaultPayloadTypeIdProperty, "Payload" }
            },
            CorrelationId = "8B4C4C3C-482A-4688-8458-AFF9998C0A12",
            SessionId = "ABB8761B-C22E-407E-801C-DFAF68916F04"
        };

        if (receiver != null)
        {
            await client.TriggerMessageReception(message, cancellationToken ?? CancellationToken.None, receiver);
            return;
        }
        await client.TriggerMessageReception(message, cancellationToken ?? CancellationToken.None);
    }

    private async Task SimulateSessionEventReception(
        SessionProcessorMock client,
        CancellationToken? cancellationToken = null,
        ServiceBusSessionReceiver receiver = null)
    {
        var parser = new TextJsonPayloadSerializer();
        var result = parser.SerializeBody(new { });
        var message = new ServiceBusMessage(result.Body)
        {
            ContentType = result.ContentType,
            Subject = "An integration event of type 'MyEvent'",
            ApplicationProperties =
            {
                { UserProperties.MessageTypeProperty, "IntegrationEvent" },
                { UserProperties.DefaultPayloadTypeIdProperty, "Payload" }
            },
            CorrelationId = "8B4C4C3C-482A-4688-8458-AFF9998C0A12",
            SessionId = "ABB8761B-C22E-407E-801C-DFAF68916F04"
        };

        if (receiver != null)
        {
            await client.TriggerMessageReception(message, cancellationToken ?? CancellationToken.None, receiver);
            return;
        }
        await client.TriggerMessageReception(message, cancellationToken ?? CancellationToken.None);
    }

    internal class Payload
    {
    }

    private class MetadataHandler : IMessageReceptionHandler<Payload>
    {
        private readonly IMessageMetadataAccessor _metadataAccessor;

        public MetadataHandler(IMessageMetadataAccessor metadataAccessor, List<IMessageMetadata> list)
        {
            _metadataAccessor = metadataAccessor;
            list.Add(metadataAccessor.Metadata);
        }

        public async Task Handle(Payload @event, CancellationToken cancellationToken)
        {
            await _metadataAccessor.Metadata.AbandonMessageAsync();
            await _metadataAccessor.Metadata.CompleteMessageAsync();
            await _metadataAccessor.Metadata.DeferMessageAsync();
            await _metadataAccessor.Metadata.DeadLetterMessageAsync();
        }
    }
}