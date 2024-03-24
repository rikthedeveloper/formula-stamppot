using System.Numerics;
using System.Text;

namespace Utils;

public static class ConvertLongBase36
{
    const string _digits = "0123456789abcdefghijklmnopqrstuvwxyz";

    public static long Decode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Empty value.");

        return TryDecode(value, out var result) 
            ? result 
            : throw new ArgumentException($"Invalid value: \"{value}\".");
    }

    public static bool TryDecode(string? value, out long result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        value = value.ToLowerInvariant();
        var negative = false;
        if (value[0] == '-')
        {
            negative = true;
            value = value[1..];
        }

        if (value.Any(c => !_digits.Contains(c)))
        {
            result = default;
            return false;
        }

        var decoded = 0L;
        for (var i = 0; i < value.Length; ++i)
        {
            decoded += _digits.IndexOf(value[i]) * (long)BigInteger.Pow(_digits.Length, value.Length - i - 1);
        }

        result = negative ? decoded * -1 : decoded;
        return true;
    }

    public static string Encode(long value)
    {
        if (value == long.MinValue)
        {
            //hard coded value due to error when getting absolute value below: "Negating the minimum value of a twos complement number is invalid.".
            return "-1y2p0ij32e8e8";
        }

        var negative = value < 0;
        value = Math.Abs(value);
        var sb = new StringBuilder();

        do
        {
            sb.Insert(0, _digits[(int)(value % _digits.Length)]);
        }
        while ((value /= _digits.Length) != 0);

        return negative ? sb.Insert(0, '-').ToString() : sb.ToString();
    }


}
