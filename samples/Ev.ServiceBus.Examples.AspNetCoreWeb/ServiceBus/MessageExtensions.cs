using Microsoft.Azure.ServiceBus;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Ev.ServiceBus.Examples.AspNetCoreWeb
{
    public static class MessageExtensions
    {
        public static T DeserializeBody<T>(this Message message)
        {
            var bf = new BinaryFormatter();
            using var stream = new MemoryStream(message.Body);
            return (T)bf.Deserialize(stream);
        }
    }
}
