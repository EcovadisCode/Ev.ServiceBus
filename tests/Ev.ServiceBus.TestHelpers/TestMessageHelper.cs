using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.TestHelpers;

public static class TestMessageHelper
{
    public static ServiceBusMessage CreateEventMessage(string payloadTypeId, object payload)
    {
        var parser = new TextJsonPayloadSerializer();
        var result = parser.SerializeBody(payload);
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, payloadTypeId);

        return message;
    }
}
