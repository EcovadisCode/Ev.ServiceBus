using System;
using System.Text;
using Ev.ServiceBus.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Ev.ServiceBus.Samples.Receiver;

public class MessagePayloadSerializer : IMessagePayloadSerializer
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.None,
        Converters =
        {
            new StringEnumConverter()
        },
        NullValueHandling = NullValueHandling.Ignore
    };

    public SerializationResult SerializeBody(object objectToSerialize)
    {
        var json = JsonConvert.SerializeObject(objectToSerialize, Formatting.None, Settings);
        return new SerializationResult("application/json", Encoding.UTF8.GetBytes(json));
    }

    public object DeSerializeBody(byte[] content, Type typeToCreate)
    {
        var @string = Encoding.UTF8.GetString(content);
        return JsonConvert.DeserializeObject(@string, typeToCreate, Settings)!;
    }
}