using System.Text.Json;
using System.Text.Json.Serialization;
using WebUI.Model.Hypermedia;

namespace WebUI.JsonConverters;

public class HypermediaJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsAssignableTo(typeof(HypermediaResource));

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) 
        => new HypermediaResourceJsonConverter();

    public class HypermediaResourceJsonConverter : JsonConverter<HypermediaResource>
    {
        public override HypermediaResource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
        public override void Write(Utf8JsonWriter writer, HypermediaResource value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("_links");
            writer.WriteRawValue(JsonSerializer.SerializeToUtf8Bytes(value.Links, options), skipInputValidation: true);
            if (value.Actions.Count > 0)
            {
                writer.WritePropertyName("_actions");
                writer.WriteRawValue(JsonSerializer.SerializeToUtf8Bytes(value.Actions, options), skipInputValidation: true);
            }

            if (value.Metadata is not null)
            {
                writer.WritePropertyName("_meta");
                writer.WriteRawValue(JsonSerializer.SerializeToUtf8Bytes(value.Metadata, options), skipInputValidation: true);
            }

            var resourceNode = JsonSerializer.SerializeToNode(value.Resource, options)?.AsObject() ?? throw new Exception();
            foreach (var node in resourceNode)
            {
                writer.WritePropertyName(node.Key);
                if (node.Value is not null)
                {
                    node.Value.WriteTo(writer);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }

            writer.WriteEndObject();
        }
    }
}
