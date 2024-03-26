using System.Text.Json;
using System.Text.Json.Serialization;
using WebUI.Filters;

namespace WebUI.JsonConverters
{
    public class ValidationMessagesJsonConverter : JsonConverter<Dictionary<string, ValidationMessage[]>>
    {
        public override Dictionary<string, ValidationMessage[]>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, ValidationMessage[]> value, JsonSerializerOptions options)
        {
            options = new JsonSerializerOptions(options)
            {
                DictionaryKeyPolicy = options.PropertyNamingPolicy,
            };
            options.Converters.Remove(this);
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
