using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions;

public static class MessageHelper
{
    public static string? GetPayloadTypeId(this ServiceBusReceivedMessage message, string? payloadTypeIdProperty)
    {
        return TryGetValue(message, payloadTypeIdProperty ?? UserProperties.DefaultPayloadTypeIdProperty);
    }

    private static string? TryGetValue(ServiceBusReceivedMessage message, string propertyName)
    {
        message.ApplicationProperties.TryGetValue(propertyName, out var value);
        return value as string;
    }

    public static ServiceBusMessage CreateMessage(string contentType, byte[] body, string payloadTypeId, string? payloadTypeIdProperty)
    {
        var message = new ServiceBusMessage(body)
        {
            ContentType = contentType,
            Subject = $"An Ev.ServiceBus message of type '{payloadTypeId}'",
            ApplicationProperties =
            {
                {UserProperties.MessageTypeProperty, "IntegrationEvent"},
                {payloadTypeIdProperty ?? UserProperties.DefaultPayloadTypeIdProperty, payloadTypeId}
            }
        };
        return message;
    }
}