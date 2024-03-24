using System.Text.Json;
using System.Text.Json.Serialization;
using WebUI.Types.Internal;

namespace WebUI.JsonConverters;

public class ParseAndFormatJsonConverter<T>(string format) : JsonConverter<T>
    where T : IFormattable, ITryParseable<string, T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => T.TryParse(reader.GetString(), out var result) ? result : throw new JsonException();

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) 
        => writer.WriteStringValue(value.ToString(format, null));
}
