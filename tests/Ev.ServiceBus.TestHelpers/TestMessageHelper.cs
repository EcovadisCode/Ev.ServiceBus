using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;

namespace Ev.ServiceBus.TestHelpers;

public static class TestMessageHelper
{
    public static ServiceBusMessage CreateEventMessage(string payloadTypeId, object payload, string payloadTypeIdProperty = UserProperties.DefaultPayloadTypeIdProperty)
    {
        var parser = new TextJsonPayloadSerializer();
        var result = parser.SerializeBody(payload);
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, payloadTypeId, payloadTypeIdProperty);

        return message;
    }
}
