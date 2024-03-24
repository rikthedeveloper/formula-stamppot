using System.Text.Json.Serialization;

namespace WebUI.Model.Hypermedia;

public record class Hyperlink([property: JsonPropertyName("href"), JsonPropertyOrder(9)] Uri Uri)
{
    public Hyperlink(string url)
        : this(new Uri(url)) { }

    [JsonPropertyName("templated"), JsonPropertyOrder(10), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsTemplated { get; init; } = false;
    [JsonPropertyName("type"), JsonPropertyOrder(11), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Type? ContentType { get; init; } = null;
    [JsonPropertyName("profile"), JsonPropertyOrder(12), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string[] Profiles { get; init; } = [];
}

public record class Action([property: JsonPropertyName("method"), JsonPropertyOrder(8)] string Method, Uri Uri) : Hyperlink(Uri)
{
    public Action(string method, string url)
        : this(method, new Uri(url)) { }
}

public class Metadata
{
    [JsonPropertyName("version"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Version { get; set; }
}

public class HypermediaResource(object resource, IReadOnlyDictionary<string, Hyperlink> links, IReadOnlyDictionary<string, Action> actions, Metadata? metadata)
{
    [JsonPropertyName("_links")] public IReadOnlyDictionary<string, Hyperlink> Links { get; } = links;
    [JsonPropertyName("_actions")] public IReadOnlyDictionary<string, Action> Actions { get; } = actions;
    [JsonIgnore] public object Resource { get; } = resource;
    [JsonPropertyName("_meta"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Metadata? Metadata { get; } = metadata;
}

public class HypermediaResource<T>(T resource, IReadOnlyDictionary<string, Hyperlink> links, IReadOnlyDictionary<string, Action> actions, Metadata? metadata) : HypermediaResource(resource, links, actions, metadata)
    where T : class
{
    [JsonIgnore] public new T Resource { get; } = resource;
}
