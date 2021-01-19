using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Ev.ServiceBus.Samples.AspNetCoreWeb.ServiceBus
{
    public static class JsonConverter
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

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.None, Settings);
        }

        public static TModel Deserialize<TModel>(string value)
        {
            return JsonConvert.DeserializeObject<TModel>(value, Settings)!;
        }
    }
}
