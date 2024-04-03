using System.Text.Json;

namespace WebUI.Domain.ObjectStore.Internal;

public class ObjectStoreJsonOptions
{
    public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions(JsonSerializerDefaults.General);
}
