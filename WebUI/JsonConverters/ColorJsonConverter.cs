using System.Text.Json.Serialization;
using System.Text.Json;
using WebUI.Types;

namespace WebUI.JsonConverters;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Color.FromHex(reader.GetString() ?? "#FFFFFF");
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToHex());
}
