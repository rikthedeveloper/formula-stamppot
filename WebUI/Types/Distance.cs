namespace WebUI.Types;

public readonly struct Distance : IEquatable<Distance>
{
    public static readonly Distance Zero = new();

    const int _centimetersFactor = 10;
    const int _metersFactor = 1000;
    const int _kilometersFactor = 1_000_000;

    readonly long _millimeters;
    Distance(long millimeters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(millimeters, nameof(millimeters));
        _millimeters = millimeters;
    }

    public static Distance FromMillimeters(long millimeters) => new(millimeters);
    public static Distance FromCentimeters(long centimeters) => new(centimeters * _centimetersFactor);
    public static Distance FromMeters(long meters) => new(meters * _metersFactor);
    public static Distance FromKilometers(long kilometers) => new(kilometers * _kilometersFactor);

    public long ToMillimeters() => _millimeters;
    public long ToCentimeters() => _millimeters / _centimetersFactor;
    public long ToMeters() => _millimeters / _metersFactor;
    public long ToKilometers() => _millimeters / _metersFactor;

    public override bool Equals(object? obj)
        => obj is Distance other && Equals(other);

    public override int GetHashCode()
        => _millimeters.GetHashCode();

    public override string ToString()
        => _millimeters.ToString();

    public bool Equals(Distance other)
        => _millimeters.Equals(other._millimeters);

    public static bool operator ==(Distance left, Distance right)
        => left.Equals(right);

    public static bool operator !=(Distance left, Distance right)
        => !(left == right);
}
