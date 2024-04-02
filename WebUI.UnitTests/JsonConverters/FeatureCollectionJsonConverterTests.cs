using FluentAssertions;
using System.Text.Json;
using WebUI.Configuration;
using WebUI.JsonConverters;
using WebUI.Types;

namespace WebUI.UnitTests.JsonConverters;

public class FeatureCollectionJsonConverterTests
{
    record class TestFeature(bool Enabled, TimeSpan Gain) : FeatureBase(Enabled);

    [Fact]
    public void CanSerializeAndDeserializeFeatureCollection()
    {
        // Arrange
        var featureRegistry = new FeatureRegistry(new Dictionary<Type, FeatureRegistration>()
    {
        { typeof(TestFeature), new FeatureRegistration(typeof(TestFeature)) }
    });

        var converter = new FeatureCollectionJsonConverter(featureRegistry);
        var options = new JsonSerializerOptions
        {
            Converters = { converter }
        };

        var originalCollection = new FeatureCollection([new TestFeature(true, new(00, 00, 05))]);

        // Act
        var json = JsonSerializer.Serialize(originalCollection, options);
        var deserializedCollection = JsonSerializer.Deserialize<FeatureCollection>(json, options);

        // Assert
        deserializedCollection.Should().NotBeNull();
        deserializedCollection.Should().BeEquivalentTo(originalCollection);
    }
}