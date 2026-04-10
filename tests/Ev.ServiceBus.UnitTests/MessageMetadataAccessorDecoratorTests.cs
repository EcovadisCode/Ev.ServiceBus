using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class MessageMetadataAccessorDecoratorTests
{
    [Fact]
    public async Task DecoratorSetDataIsCalledDuringReception()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.AddSingleton<DecoratorCallTracker>();
            services.Decorate<IMessageMetadataAccessor, TrackingMetadataAccessorDecorator>();
            services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
            {
                builder.RegisterReception<Payload, NoopHandler>();
            });
        });

        var provider = await composer.Compose();
        var clientMock = provider.GetProcessorMock("testQueue");

        await TriggerReception(clientMock);

        var tracker = provider.GetRequiredService<DecoratorCallTracker>();
        tracker.SetDataCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DecoratorMetadataIsAccessibleFromHandler()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.AddSingleton<DecoratorCallTracker>();
            services.AddSingleton<List<IMessageMetadata?>>();
            services.Decorate<IMessageMetadataAccessor, TrackingMetadataAccessorDecorator>();
            services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
            {
                builder.RegisterReception<Payload, MetadataCaptureHandler>();
            });
        });

        var provider = await composer.Compose();
        var clientMock = provider.GetProcessorMock("testQueue");

        await TriggerReception(clientMock);

        var captured = provider.GetRequiredService<List<IMessageMetadata?>>();
        captured.Count.Should().Be(1);
        captured[0].Should().NotBeNull();
        captured[0]!.Subject.Should().Be("test subject");
    }

    [Fact]
    public async Task DecoratorSetDataIsCalledDuringSessionReception()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.AddSingleton<DecoratorCallTracker>();
            services.Decorate<IMessageMetadataAccessor, TrackingMetadataAccessorDecorator>();
            services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
            {
                builder.EnableSessionHandling(options => { });
                builder.RegisterReception<Payload, NoopHandler>();
            });
        });

        var provider = await composer.Compose();
        var clientMock = provider.GetSessionProcessorMock("testQueue");

        await TriggerSessionReception(clientMock);

        var tracker = provider.GetRequiredService<DecoratorCallTracker>();
        tracker.SetDataCallCount.Should().Be(1);
    }

    private static async Task TriggerReception(ProcessorMock client, CancellationToken cancellationToken = default)
    {
        var serializer = new TextJsonPayloadSerializer();
        var body = serializer.SerializeBody(new { });
        var message = new ServiceBusMessage(body.Body)
        {
            ContentType = body.ContentType,
            Subject = "test subject",
            ApplicationProperties =
            {
                { UserProperties.MessageTypeProperty, "IntegrationEvent" },
                { UserProperties.PayloadTypeIdProperty, "Payload" }
            }
        };
        await client.TriggerMessageReception(message, cancellationToken);
    }

    private static async Task TriggerSessionReception(SessionProcessorMock client, CancellationToken cancellationToken = default)
    {
        var serializer = new TextJsonPayloadSerializer();
        var body = serializer.SerializeBody(new { });
        var message = new ServiceBusMessage(body.Body)
        {
            ContentType = body.ContentType,
            Subject = "test subject",
            ApplicationProperties =
            {
                { UserProperties.MessageTypeProperty, "IntegrationEvent" },
                { UserProperties.PayloadTypeIdProperty, "Payload" }
            }
        };
        await client.TriggerMessageReception(message, cancellationToken);
    }

    internal class Payload { }

    private class NoopHandler : IMessageReceptionHandler<Payload>
    {
        public Task Handle(Payload @event, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class MetadataCaptureHandler : IMessageReceptionHandler<Payload>
    {
        private readonly IMessageMetadataAccessor _accessor;
        private readonly List<IMessageMetadata?> _captured;

        public MetadataCaptureHandler(IMessageMetadataAccessor accessor, List<IMessageMetadata?> captured)
        {
            _accessor = accessor;
            _captured = captured;
        }

        public Task Handle(Payload @event, CancellationToken cancellationToken)
        {
            _captured.Add(_accessor.Metadata);
            return Task.CompletedTask;
        }
    }

    internal class DecoratorCallTracker
    {
        public int SetDataCallCount { get; private set; }
        public void RecordSetData() => SetDataCallCount++;
    }

    private class TrackingMetadataAccessorDecorator : IMessageMetadataAccessor
    {
        private readonly IMessageMetadataAccessor _inner;
        private readonly DecoratorCallTracker _tracker;

        public TrackingMetadataAccessorDecorator(IMessageMetadataAccessor inner, DecoratorCallTracker tracker)
        {
            _inner = inner;
            _tracker = tracker;
        }

        public IMessageMetadata? Metadata => _inner.Metadata;

        public void SetData(MessageContext context)
        {
            _tracker.RecordSetData();
            _inner.SetData(context);
        }
    }
}