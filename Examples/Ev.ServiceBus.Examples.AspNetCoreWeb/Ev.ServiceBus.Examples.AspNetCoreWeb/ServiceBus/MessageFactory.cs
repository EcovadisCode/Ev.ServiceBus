using Microsoft.Azure.ServiceBus;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Ev.ServiceBus.Examples.AspNetCoreWeb
{
    public class MessageFactory
    {
        public Message Serialize(object obj)
        {
            var bf = new BinaryFormatter();
            using var ms = new MemoryStream();

            bf.Serialize(ms, obj);
            return new Message(ms.ToArray())
            {
                ContentType = "application/octet-stream"
            };
        }
    }
}
