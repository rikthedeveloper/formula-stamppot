namespace WebUI.Extensions;

public static class StringExtensions
{
    public static string TrimFromStart(this string s, string value)
        => TrimFromStart(s, value, StringComparison.CurrentCulture);

    public static string TrimFromEnd(this string s, string value)
        => TrimFromEnd(s, value, StringComparison.CurrentCulture);

    public static string TrimFromStart(this string s, string value, StringComparison comparisonType)
        => s.StartsWith(value, comparisonType) ? s[value.Length..] : s;

    public static string TrimFromEnd(this string s, string value, StringComparison comparisonType)
        => s.EndsWith(value, comparisonType) ? s[..^value.Length] : s;
}
