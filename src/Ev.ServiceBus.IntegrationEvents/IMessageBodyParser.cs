using System;

namespace Ev.ServiceBus.IntegrationEvents
{
    public interface IMessageBodyParser
    {
        public SerializationResult SerializeBody(object objectToSerialize);
        public object DeSerializeBody(byte[] content, Type typeToCreate);
    }

    public sealed class SerializationResult
    {
        public string ContentType { get; }
        public byte[] Body { get; }

        public SerializationResult(string contentType, byte[] body)
        {
            ContentType = contentType;
            Body = body;
        }
    }
}
