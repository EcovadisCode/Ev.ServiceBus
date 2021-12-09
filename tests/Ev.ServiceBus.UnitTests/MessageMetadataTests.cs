using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class MessageMetadataTests
    {
        [Fact]
        public async Task AScopeIsCreatedForEachMessageReceived()
        {
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

            var clientMock = provider.GetQueueClientMock("testQueue");

            await SimulateEventReception(clientMock);

            var metadatas = provider.GetRequiredService<List<IMessageMetadata>>();
            metadatas.Count.Should().Be(1);

            var metadata = metadatas[0];
            metadata.Label.Should().Be("An integration event of type 'MyEvent'");
            metadata.ContentType.Should().Be("application/json");
            metadata.UserProperties.Keys.Should()
                .Contain(UserProperties.MessageTypeProperty)
                .And
                .Contain(UserProperties.EventTypeIdProperty);
        }

        private async Task SimulateEventReception(
            QueueClientMock client,
            CancellationToken? cancellationToken = null)
        {
            var parser = new PayloadSerializer();
            var result = parser.SerializeBody(new { });
            var message = new Message(result.Body)
            {
                ContentType = result.ContentType,
                Label = "An integration event of type 'MyEvent'",
                UserProperties =
                {
                    { UserProperties.MessageTypeProperty, "IntegrationEvent" },
                    { UserProperties.EventTypeIdProperty, "Payload" }
                }
            };

            // Necessary to simulate the reception of the message
            var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(message.SystemProperties, 1, null);
            }

            await client.TriggerMessageReception(message, cancellationToken ?? CancellationToken.None);
        }

        internal class Payload
        {
        }

        private class MetadataHandler : IMessageReceptionHandler<Payload>
        {

            public MetadataHandler(IMessageMetadataAccessor metadataAccessor, List<IMessageMetadata> list)
            {
                list.Add(metadataAccessor.Metadata);
            }

            public Task Handle(Payload @event, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }

}
