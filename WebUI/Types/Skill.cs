using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace WebUI.Types;

public readonly struct Skill : IEquatable<Skill>, IComparable<Skill>,
    IEqualityOperators<Skill, Skill, bool>
{
    public Skill(BoundedPercentage minimum, BoundedPercentage maximum)
    {
        if (minimum > maximum)
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The minimum value of a grade must be smaller than the maximum value.");

        Minimum = minimum;
        Maximum = maximum;
        RangeBetween = (BoundedPercentage)(maximum - minimum);
    }

    public readonly BoundedPercentage Minimum;
    public readonly BoundedPercentage Maximum;
    public readonly BoundedPercentage RangeBetween;

    public int CompareTo(Skill other) => Math.Clamp(Minimum.CompareTo(other.Minimum) + Maximum.CompareTo(other.Maximum) + RangeBetween.CompareTo(other.RangeBetween), -1, 1);
    public bool Equals(Skill other) => Minimum.Equals(other.Minimum) && Maximum.Equals(other.Maximum);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Skill other && Equals(other);
    public override int GetHashCode() => Minimum.GetHashCode() ^ Maximum.GetHashCode();

    public static bool operator <(Skill left, Skill right) => left.CompareTo(right) < 0;
    public static bool operator <=(Skill left, Skill right) => left.CompareTo(right) <= 0;
    public static bool operator >(Skill left, Skill right) => left.CompareTo(right) > 0;
    public static bool operator >=(Skill left, Skill right) => left.CompareTo(right) >= 0;

    public static bool operator ==(Skill left, Skill right) => left.Equals(right);
    public static bool operator !=(Skill left, Skill right) => !(left == right);
}
