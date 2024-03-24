using Utils;
using WebUI.Types.Internal;

namespace WebUI.Types;

public readonly record struct TrackId(long Value) : IEquatable<TrackId>, ITryParseable<string, TrackId>, IFormattable
{
    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString()
        => Value.ToString();

    public static bool TryParse(string? value, out TrackId @out)
    {
        if (long.TryParse(value, out var result))
        {
            @out = new(result);
            return true;
        }

        if (ConvertLongBase36.TryDecode(value, out result))
        {
            @out = new(result);
            return true;
        }

        @out = default;
        return false;
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        format ??= string.Empty;
        return format.ToUpperInvariant() switch
        {
            "BASE36" => ConvertLongBase36.Encode(Value),
            _ => Value.ToString(format, formatProvider),
        };
    }

    public static implicit operator long(TrackId value) => value.Value;
}
