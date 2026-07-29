namespace Djurspel.Core;

/// <summary>
/// Immutable 2D vector with float components.
/// </summary>
public readonly struct Vec2
{
    public float X { get; }
    public float Y { get; }

    public Vec2(float x, float y) { X = x; Y = y; }

    /// <summary>Zero vector (0, 0).</summary>
    public static Vec2 Zero => new(0, 0);

    /// <summary>Unit vector (1, 1).</summary>
    public static Vec2 One => new(1, 1);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 v, float s) => new(v.X * s, v.Y * s);
    public static Vec2 operator /(Vec2 v, float s) => s != 0 ? new(v.X / s, v.Y / s) : Zero;
    public static bool operator ==(Vec2 a, Vec2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vec2 a, Vec2 b) => !(a == b);

    /// <summary>Euclidean length (magnitude).</summary>
    public float Length => MathF.Sqrt(X * X + Y * Y);

    /// <summary>Squared length (avoids sqrt for comparisons).</summary>
    public float LengthSquared => X * X + Y * Y;

    /// <summary>Normalized vector (length = 1).</summary>
    public Vec2 Normalized
    {
        get
        {
            float len = Length;
            return len > 0 ? this * (1f / len) : Vec2.Zero;
        }
    }

    /// <summary>Dot product with another Vec2.</summary>
    public float Dot(Vec2 other) => X * other.X + Y * other.Y;

    /// <summary>Distance to another Vec2.</summary>
    public float DistanceTo(Vec2 other) => (this - other).Length;

    /// <summary>Clamp components to [min, max].</summary>
    public Vec2 Clamp(float min, float max) => new(MathF.Max(min, MathF.Min(X, max)), MathF.Max(min, MathF.Min(Y, max)));

    /// <summary>Round to integer coordinates.</summary>
    public Vec2I Round() => new((int)MathF.Round(X), (int)MathF.Round(Y));

    public override string ToString() => $"({X}, {Y})";
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override bool Equals(object? obj) => obj is Vec2 other && this == other;
}

/// <summary>
/// Immutable 3D vector with float components.
/// </summary>
public readonly struct Vec3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

    /// <summary>Zero vector (0, 0, 0).</summary>
    public static Vec3 Zero => new(0, 0, 0);

    /// <summary>Up vector (0, 1, 0).</summary>
    public static Vec3 Up => new(0, 1, 0);

    /// <summary>Forward vector (0, 0, 1) in world space.</summary>
    public static Vec3 Forward => new(0, 0, 1);

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vec3 operator /(Vec3 v, float s) => s != 0 ? new(v.X / s, v.Y / s, v.Z / s) : Zero;
    public static bool operator ==(Vec3 a, Vec3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Vec3 a, Vec3 b) => !(a == b);

    /// <summary>Euclidean length.</summary>
    public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Squared length.</summary>
    public float LengthSquared => X * X + Y * Y + Z * Z;

    /// <summary>Normalized vector (length = 1).</summary>
    public Vec3 Normalized
    {
        get
        {
            float len = Length;
            return len > 0 ? this * (1f / len) : Vec3.Zero;
        }
    }

    /// <summary>Dot product.</summary>
    public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>Cross product.</summary>
    public static Vec3 Cross(Vec3 a, Vec3 b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    /// <summary>Distance to another Vec3.</summary>
    public float DistanceTo(Vec3 other) => (this - other).Length;

    /// <summary>Lerp between this and other by t in [0,1].</summary>
    public Vec3 Lerp(Vec3 other, float t) => new(
        X + (other.X - X) * t,
        Y + (other.Y - Y) * t,
        Z + (other.Z - Z) * t
    );

    public override string ToString() => $"({X}, {Y}, {Z})";
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override bool Equals(object? obj) => obj is Vec3 other && this == other;
}

/// <summary>
/// Immutable 2D vector with integer components.
/// Used for grid/tile coordinates.
/// </summary>
public readonly struct Vec2I
{
    public int X { get; }
    public int Y { get; }

    public Vec2I(int x, int y) { X = x; Y = y; }

    public static Vec2I Zero => new(0, 0);
    public static Vec2I One => new(1, 1);

    public static Vec2I operator +(Vec2I a, Vec2I b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2I operator -(Vec2I a, Vec2I b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2I operator *(Vec2I v, int s) => new(v.X * s, v.Y * s);
    public static Vec2I operator /(Vec2I v, int s) => s != 0 ? new(v.X / s, v.Y / s) : Zero;
    public static bool operator ==(Vec2I a, Vec2I b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vec2I a, Vec2I b) => !(a == b);

    /// <summary>Convert to float Vec2.</summary>
    public Vec2 ToFloat() => new(X, Y);

    public override string ToString() => $"({X}, {Y})";
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override bool Equals(object? obj) => obj is Vec2I other && this == other;
}

/// <summary>
/// Immutable 3D vector with integer components.
/// Used for tile coordinates (x, y, z/height).
/// </summary>
public readonly struct Vec3I
{
    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public Vec3I(int x, int y, int z) { X = x; Y = y; Z = z; }

    public static Vec3I Zero => new(0, 0, 0);

    public static Vec3I operator +(Vec3I a, Vec3I b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3I operator -(Vec3I a, Vec3I b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3I operator *(Vec3I v, int s) => new(v.X * s, v.Y * s, v.Z * s);
    public static bool operator ==(Vec3I a, Vec3I b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Vec3I a, Vec3I b) => !(a == b);

    /// <summary>Convert to float Vec3.</summary>
    public Vec3 ToFloat() => new(X, Y, Z);

    public override string ToString() => $"({X}, {Y}, {Z})";
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override bool Equals(object? obj) => obj is Vec3I other && this == other;
}

/// <summary>
/// Immutable RGBA color with byte components (0-255).
/// </summary>
public readonly struct Color
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }

    /// <summary>White (255, 255, 255, 255).</summary>
    public static Color White => new(255, 255, 255);

    /// <summary>Black (0, 0, 0, 255).</summary>
    public static Color Black => new(0, 0, 0);

    /// <summary>Transparent (0, 0, 0, 0).</summary>
    public static Color Transparent => new(0, 0, 0, 0);

    /// <summary>Red.</summary>
    public static Color Red => new(255, 0, 0);

    /// <summary>Green.</summary>
    public static Color Green => new(0, 255, 0);

    /// <summary>Blue.</summary>
    public static Color Blue => new(0, 0, 255);

    /// <summary>Yellow.</summary>
    public static Color Yellow => new(255, 255, 0);

    /// <summary>Gray.</summary>
    public static Color Gray => new(128, 128, 128);

    /// <summary>Dark gray.</summary>
    public static Color DarkGray => new(64, 64, 64);

    /// <summary>Light gray.</summary>
    public static Color LightGray => new(192, 192, 192);

    /// <summary>Orange.</summary>
    public static Color Orange => new(255, 165, 0);

    /// <summary>Purple.</summary>
    public static Color Purple => new(128, 0, 128);

    /// <summary>Pink.</summary>
    public static Color Pink => new(255, 192, 203);

    /// <summary>Teal.</summary>
    public static Color Teal => new(0, 128, 128);

    /// <summary>Brown.</summary>
    public static Color Brown => new(139, 69, 19);

    /// <summary>Olive green (isometric ground color).</summary>
    public static Color Olive => new(107, 142, 35);

    /// <summary>Flat color with full alpha. Used for OpenGL.</summary>
    public static Color Flat(byte r, byte g, byte b) => new(r, g, b, 255);

    /// <summary>ARGB from a single hex value.</summary>
    public static Color FromArgb(int argb) => new(
        (byte)((argb >> 24) & 0xFF),
        (byte)((argb >> 16) & 0xFF),
        (byte)((argb >> 8) & 0xFF),
        (byte)(argb & 0xFF)
    );

     /// <summary>ARGB from RGB (full opacity).</summary>
    public static Color FromRgb(int r, int g, int b)
    {
        uint argb = ((uint)255 << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
        return FromArgb((int)argb);
    }

    /// <summary>Convert to float array [r, g, b, a] normalized to [0, 1].</summary>
    public float[] ToFloatArray() => new[] { R / 255f, G / 255f, B / 255f, A / 255f };

    /// <summary>Multiply this color by another (component-wise).</summary>
    public Color Multiply(Color other) => new(
        (byte)(R * other.R / 255),
        (byte)(G * other.G / 255),
        (byte)(B * other.B / 255),
        (byte)(A * other.A / 255)
    );

    /// <summary>Slightly darken this color by the given factor.</summary>
    public Color Darken(float factor)
    {
        int f = (int)(255f * factor);
        return new Color(
            (byte)(R * f / 255),
            (byte)(G * f / 255),
            (byte)(B * f / 255)
        );
    }

    /// <summary>Slightly lighten this color by the given factor.</summary>
    public Color Lighten(float factor)
    {
        int f = (int)(255f * factor);
        return new Color(
            (byte)(R * f / 255),
            (byte)(G * f / 255),
            (byte)(B * f / 255)
        );
    }

    public override string ToString() => $"Color({R},{G},{B},{A})";
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override bool Equals(object? obj) => obj is Color other && this == other;

    public static bool operator ==(Color a, Color b) =>
        a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;

    public static bool operator !=(Color a, Color b) => !(a == b);
}
