using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.TestHelpers;

public static class TestMessageHelper
{
    public static Message CreateEventMessage(string payloadTypeId, object payload)
    {
        var parser = new PayloadSerializer();
        var result = parser.SerializeBody(payload);
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, payloadTypeId);

        // Necessary to simulate the reception of the message
        var propertyInfo = message.SystemProperties.GetType().GetProperty("SequenceNumber");
        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            propertyInfo.SetValue(message.SystemProperties, 1, null);
        }

        return message;
    }
}
