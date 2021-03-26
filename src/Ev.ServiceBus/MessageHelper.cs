using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus
{
    public static class MessageHelper
    {
        internal static string? GetEventTypeId(this Message message)
        {
            return TryGetValue(message, UserProperties.EventTypeIdProperty);
        }

        internal static string? GetPayloadTypeId(this Message message)
        {
            return TryGetValue(message, UserProperties.PayloadTypeIdProperty);
        }

        private static string? TryGetValue(Message message, string propertyName)
        {
            message.UserProperties.TryGetValue(propertyName, out var value);
            return value as string;
        }

        internal static Message CreateMessage(string contentType, byte[] body, string payloadTypeId)
        {
            var message = new Message(body)
            {
                ContentType = contentType,
                Label = $"An Ev.ServiceBus message of type '{payloadTypeId}'",
                UserProperties =
                {
                    {UserProperties.MessageTypeProperty, "IntegrationEvent"},
                    {UserProperties.PayloadTypeIdProperty, payloadTypeId},
                    {UserProperties.EventTypeIdProperty, payloadTypeId}
                }
            };
            return message;
        }
    }
}
