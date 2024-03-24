using System.Text.Json;
using System.Text.Json.Serialization;
using WebUI.Types;

namespace WebUI.JsonConverters;

public class DistanceJsonConverter : JsonConverter<Distance>
{
    public override Distance Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Distance.FromMillimeters(reader.GetInt64());
    public override void Write(Utf8JsonWriter writer, Distance value, JsonSerializerOptions options) => writer.WriteNumberValue(value.ToMillimeters());
}
