using System.Text;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Samples.AspNetCoreWeb.ServiceBus
{
    public static class MessageParser
    {
        public static Message SerializeMessage(object payload)
        {
            var jsonBody = JsonConverter.Serialize(payload);

            var message = new Message(Encoding.UTF8.GetBytes(jsonBody))
            {
                ContentType = "application/json"
            };

            return message;
        }

        public static T DeserializeMessage<T>(Message message)
        {
            var body = Encoding.UTF8.GetString(message.Body);
            var deserializedObject = JsonConverter.Deserialize<T>(body);

            return deserializedObject;
        }
    }
}
