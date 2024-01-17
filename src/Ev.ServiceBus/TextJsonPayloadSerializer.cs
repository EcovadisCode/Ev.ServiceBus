using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus;

public class TextJsonPayloadSerializer : IMessagePayloadSerializer
{
    internal static readonly JsonSerializerOptions Settings = new JsonSerializerOptions()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
    };

    public SerializationResult SerializeBody(object objectToSerialize)
    {
        var json = JsonSerializer.Serialize(objectToSerialize, Settings);
        return new SerializationResult("application/json", Encoding.UTF8.GetBytes(json));
    }

    public object DeSerializeBody(byte[] content, Type typeToCreate)
    {
        var @string = Encoding.UTF8.GetString(content);
        return JsonSerializer.Deserialize(@string, typeToCreate, Settings);
    }
}
