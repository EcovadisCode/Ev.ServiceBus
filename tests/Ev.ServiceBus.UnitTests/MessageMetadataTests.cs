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
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class MessageMetadataTests
    {
        [Fact]
        public async Task MetadataIsProperlySet()
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

            var clientMock = provider.GetProcessorMock("testQueue");

            await SimulateEventReception(clientMock);

            var metadatas = provider.GetRequiredService<List<IMessageMetadata>>();
            metadatas.Count.Should().Be(1);

            var metadata = metadatas[0];
            metadata.Label.Should().Be("An integration event of type 'MyEvent'");
            metadata.ContentType.Should().Be("application/json");
            metadata.ApplicationProperties.Keys.Should()
                .Contain(UserProperties.MessageTypeProperty)
                .And
                .Contain(UserProperties.EventTypeIdProperty);
            metadata.CorrelationId.Should().Be("8B4C4C3C-482A-4688-8458-AFF9998C0A12");
            metadata.SessionId.Should().Be("ABB8761B-C22E-407E-801C-DFAF68916F04");
        }

        private async Task SimulateEventReception(
            ProcessorMock client,
            CancellationToken? cancellationToken = null)
        {
            var parser = new PayloadSerializer();
            var result = parser.SerializeBody(new { });
            var message = new ServiceBusMessage(result.Body)
            {
                ContentType = result.ContentType,
                Subject = "An integration event of type 'MyEvent'",
                ApplicationProperties =
                {
                    { UserProperties.MessageTypeProperty, "IntegrationEvent" },
                    { UserProperties.EventTypeIdProperty, "Payload" }
                },
                CorrelationId = "8B4C4C3C-482A-4688-8458-AFF9998C0A12",
                SessionId = "ABB8761B-C22E-407E-801C-DFAF68916F04"
            };

            // Necessary to simulate the reception of the message
            // var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
            // if (propertyInfo != null && propertyInfo.CanWrite)
            // {
            //     propertyInfo.SetValue(message.SystemProperties, 1, null);
            // }

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
