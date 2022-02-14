using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.UnitTests.Helpers;

namespace Ev.ServiceBus.TestHelpers;

public static class TestMessageHelper
{
    public static ServiceBusMessage CreateEventMessage(string payloadTypeId, object payload)
    {
        var parser = new PayloadSerializer();
        var result = parser.SerializeBody(payload);
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, payloadTypeId);

        // Necessary to simulate the reception of the message
        // var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
        // if (propertyInfo != null && propertyInfo.CanWrite)
        // {
        //     propertyInfo.SetValue(message.SystemProperties, 1, null);
        // }

        return message;
    }
}
