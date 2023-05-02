using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;

namespace Ev.ServiceBus.TestHelpers;

public static class TestMessageHelper
{
    public static ServiceBusMessage CreateEventMessage(string payloadTypeId, object payload)
    {
        var parser = new PayloadSerializer();
        var result = parser.SerializeBody(payload);
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, payloadTypeId);

        return message;
    }
}
