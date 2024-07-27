using System.Drawing;

namespace WebUI.Types;

public readonly struct Color : IEquatable<Color>
{
    readonly System.Drawing.Color _internalColor;

    Color(System.Drawing.Color color)
    {
        _internalColor = color;
    }

    public byte A => _internalColor.A;
    public byte R => _internalColor.R;
    public byte G => _internalColor.G;
    public byte B => _internalColor.B;

    public static Color FromArgb(int red, int green, int blue) => new(System.Drawing.Color.FromArgb(red, green, blue));
    public static Color FromArgb(int argb) => new(System.Drawing.Color.FromArgb(argb));
    public static Color FromHex(string hex) => new(ColorTranslator.FromHtml(hex));
    public int ToArgb() => _internalColor.ToArgb();
    public string ToHex() => ColorTranslator.ToHtml(_internalColor);

    public static Color FromSystem(System.Drawing.Color color) => new(color);
    public System.Drawing.Color ToSystem() => _internalColor;

    #region IEquatable implementation

    public bool Equals(Color other)
        => _internalColor.Equals(other._internalColor);

    public override bool Equals(object? obj)
        => obj is Color color && Equals(color);

    public static bool operator ==(Color left, Color right)
        => left.Equals(right);

    public static bool operator !=(Color left, Color right)
        => !(left == right);

    public override int GetHashCode()
        => _internalColor.GetHashCode();

    #endregion
}
