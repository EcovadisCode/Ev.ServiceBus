using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.IntegrationEvents
{
    public static class MessageHelper
    {
        internal static string? GetEventTypeId(this Message message)
        {
            return TryGetValue(message, UserProperties.EventTypeIdProperty);
        }

        private static string? TryGetValue(Message message, string propertyName)
        {
            message.UserProperties.TryGetValue(propertyName, out var value);
            return value as string;
        }

        internal static Message CreateMessage(string contentType, byte[] body, string eventTypeId)
        {
            var message = new Message(body)
            {
                ContentType = contentType,
                Label = $"An integration event of type '{eventTypeId}'",
                UserProperties =
                {
                    {UserProperties.MessageTypeProperty, "IntegrationEvent"},
                    {UserProperties.EventTypeIdProperty, eventTypeId}
                }
            };
            return message;
        }
    }
}
