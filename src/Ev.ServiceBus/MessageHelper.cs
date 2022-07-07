using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus;

public static class MessageHelper
{
    internal static string? GetPayloadTypeId(this ServiceBusReceivedMessage message)
    {
        return TryGetValue(message, UserProperties.PayloadTypeIdProperty);
    }

    private static string? TryGetValue(ServiceBusReceivedMessage message, string propertyName)
    {
        message.ApplicationProperties.TryGetValue(propertyName, out var value);
        return value as string;
    }

    internal static ServiceBusMessage CreateMessage(string contentType, byte[] body, string payloadTypeId)
    {
        var message = new ServiceBusMessage(body)
        {
            ContentType = contentType,
            Subject = $"An Ev.ServiceBus message of type '{payloadTypeId}'",
            ApplicationProperties =
            {
                {UserProperties.MessageTypeProperty, "IntegrationEvent"},
                {UserProperties.PayloadTypeIdProperty, payloadTypeId}
            }
        };
        return message;
    }
}