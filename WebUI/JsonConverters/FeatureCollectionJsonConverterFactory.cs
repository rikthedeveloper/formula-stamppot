using System.Text.Json;
using System.Text.Json.Serialization;
using WebUI.Configuration;
using WebUI.Types;

namespace WebUI.JsonConverters;

public class FeatureCollectionJsonConverter(FeatureRegistry featureRegistry) : JsonConverter<FeatureCollection>
{
    readonly IDictionary<string, Type> _featureTypesByName = featureRegistry.Registrations.ToDictionary(reg => reg.Key.Name, reg => reg.Key);
    readonly IDictionary<Type, string> _featureNamesByType = featureRegistry.Registrations.ToDictionary(reg => reg.Key, reg => reg.Key.Name);

    public override FeatureCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new Exception();
        }

        reader.Read(); // Reads past the StartObject token of the Dictionary.
        var result = new Dictionary<Type, IFeature>();
        while (reader.TokenType is JsonTokenType.PropertyName)
        {
            var propName = reader.GetString() ?? throw new Exception();
            var featureType = _featureTypesByName[propName];
            reader.Read(); //  // Reads to the StartObject token of the Feature.
            result.Add(featureType, (IFeature)(JsonSerializer.Deserialize(ref reader, featureType, options) ?? throw new Exception()));
        }

        if (result.Count > 0)
        {
            reader.Read(); // Reads to the EndObject token of the Dictionary.
        }

        return new(result);
    }

    public override void Write(Utf8JsonWriter writer, FeatureCollection value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var feature in value)
        {
            var type = feature.GetType();
            var featureName = _featureNamesByType[type];
            writer.WritePropertyName(featureName);
            writer.WriteRawValue(JsonSerializer.Serialize(feature, type, options));
        }
        writer.WriteEndObject();
    }
}