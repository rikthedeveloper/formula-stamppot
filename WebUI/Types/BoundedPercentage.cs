using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace WebUI.Types;

/// <summary>
/// Defines a percentage value bound between 0% and 100%.
/// </summary>
public readonly struct BoundedPercentage : IEquatable<BoundedPercentage>, IComparable<BoundedPercentage>,
    IAdditionOperators<BoundedPercentage, BoundedPercentage, UnboundPercentage>,
    ISubtractionOperators<BoundedPercentage, BoundedPercentage, UnboundPercentage>,
    IMinMaxValue<BoundedPercentage>
{
    /// <summary>
    /// Creates a new <see cref="BoundedPercentage"/>.
    /// </summary>
    /// <param name="value">The percentage value, expressed as a decimal between 0 and 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public BoundedPercentage(decimal value)
    {
        if (value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 1.");
        }

        Value = value;
    }

    public decimal Value { get; }

    public decimal Of(decimal value) => value * Value;
    public decimal Of(short value) => value * Value;
    public decimal Of(int value) => value * Value;
    public decimal Of(long value) => value * Value;
    public decimal Of(ushort value) => value * Value;
    public decimal Of(uint value) => value * Value;
    public decimal Of(ulong value) => value * Value;
    public decimal Of(float value) => Convert.ToDecimal(value) * Value;
    public decimal Of(double value) => Convert.ToDecimal(value) * Value;
    public TimeSpan Of(TimeSpan value) => TimeSpan.FromTicks(Convert.ToInt64(Math.Ceiling(value.Ticks * Value)));

    public static BoundedPercentage MaxValue { get; } = new(0);
    public static BoundedPercentage MinValue { get; } = new(1);

    public int CompareTo(BoundedPercentage other) => Value.CompareTo(other.Value);
    public bool Equals(BoundedPercentage other) => Value.Equals(other.Value);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is BoundedPercentage other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{Value * 100}%";

    public static bool operator ==(BoundedPercentage left, BoundedPercentage right) => left.Equals(right);
    public static bool operator !=(BoundedPercentage left, BoundedPercentage right) => !(left == right);
    public static bool operator <(BoundedPercentage left, BoundedPercentage right) => left.CompareTo(right) < 0;
    public static bool operator <=(BoundedPercentage left, BoundedPercentage right) => left.CompareTo(right) <= 0;
    public static bool operator >(BoundedPercentage left, BoundedPercentage right) => left.CompareTo(right) > 0;
    public static bool operator >=(BoundedPercentage left, BoundedPercentage right) => left.CompareTo(right) >= 0;

    public static UnboundPercentage operator +(BoundedPercentage left, BoundedPercentage right) => new(left.Value + right.Value);
    public static UnboundPercentage operator -(BoundedPercentage left, BoundedPercentage right) => new(left.Value - right.Value);

    public static implicit operator UnboundPercentage(BoundedPercentage value) => new(value.Value);
}

/// <summary>
/// Defines an unbound percentage value.
/// </summary>
public readonly struct UnboundPercentage : IEquatable<UnboundPercentage>, IComparable<UnboundPercentage>,
    IAdditionOperators<UnboundPercentage, UnboundPercentage, UnboundPercentage>,
    ISubtractionOperators<UnboundPercentage, UnboundPercentage, UnboundPercentage>,
    IMinMaxValue<UnboundPercentage>
{
    /// <summary>
    /// Creates a new <see cref="BoundedPercentage"/>.
    /// </summary>
    /// <param name="value">The percentage value, expressed as a decimal.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public UnboundPercentage(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public decimal Of(decimal value) => value * Value;
    public decimal Of(short value) => value * Value;
    public decimal Of(int value) => value * Value;
    public decimal Of(long value) => value * Value;
    public decimal Of(ushort value) => value * Value;
    public decimal Of(uint value) => value * Value;
    public decimal Of(ulong value) => value * Value;
    public decimal Of(float value) => Convert.ToDecimal(value) * Value;
    public decimal Of(double value) => Convert.ToDecimal(value) * Value;
    public TimeSpan Of(TimeSpan value) => TimeSpan.FromTicks(Convert.ToInt64(Math.Ceiling(value.Ticks * Value)));

    public int CompareTo(UnboundPercentage other) => Value.CompareTo(other.Value);
    public bool Equals(UnboundPercentage other) => Value.Equals(other.Value);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is UnboundPercentage other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{Value * 100}%";

    public static UnboundPercentage MaxValue { get; } = new(decimal.MaxValue);
    public static UnboundPercentage MinValue { get; } = new(decimal.MinValue);

    public static bool operator ==(UnboundPercentage left, UnboundPercentage right) => left.Equals(right);
    public static bool operator !=(UnboundPercentage left, UnboundPercentage right) => !(left == right);
    public static bool operator <(UnboundPercentage left, UnboundPercentage right) => left.CompareTo(right) < 0;
    public static bool operator <=(UnboundPercentage left, UnboundPercentage right) => left.CompareTo(right) <= 0;
    public static bool operator >(UnboundPercentage left, UnboundPercentage right) => left.CompareTo(right) > 0;
    public static bool operator >=(UnboundPercentage left, UnboundPercentage right) => left.CompareTo(right) >= 0;

    public static UnboundPercentage operator +(UnboundPercentage left, UnboundPercentage right) => new(left.Value + right.Value);
    public static UnboundPercentage operator -(UnboundPercentage left, UnboundPercentage right) => new(left.Value - right.Value);

    public static explicit operator BoundedPercentage(UnboundPercentage value) => new(value.Value);
}