using System;

namespace Ev.ServiceBus.Abstractions
{
    /// <summary>
    /// Interface to implement when you setup Service Bus.
    /// Takes care of serializing/deserializing messages.
    /// </summary>
    public interface IMessagePayloadSerializer
    {
        /// <summary>
        /// Called each time Ev.ServiceBus serializes an object to a message's payload.
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        public SerializationResult SerializeBody(object objectToSerialize);

        /// <summary>
        /// Called each time Ev.ServiceBus deserializes a message's payload.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="typeToCreate"></param>
        /// <returns></returns>
        public object DeSerializeBody(byte[] content, Type typeToCreate);
    }

    /// <summary>
    /// The result of a message's serialization
    /// </summary>
    public sealed class SerializationResult
    {
        /// <summary>
        /// The MIME type of the serialized object.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// The serialized object's data.
        /// </summary>
        public byte[] Body { get; }

        public SerializationResult(string contentType, byte[] body)
        {
            ContentType = contentType;
            Body = body;
        }
    }
}
