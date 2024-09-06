using System.Text.Json.Serialization;

namespace WebUI.Types;

public readonly record struct NameToken(
    string Name, 
    [property:JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool HasEmphasis = false);
