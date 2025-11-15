namespace Hoi4BlueprintEditor.Models.Focus;

public readonly struct FocusPoint(int x, int y) : IEquatable<FocusPoint>
{
    public int X { get; } = x;
    public int Y { get; } = y;

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public bool Equals(FocusPoint other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is FocusPoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (X * 397) ^ Y;
        }
    }

    public static bool operator ==(FocusPoint left, FocusPoint right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FocusPoint left, FocusPoint right)
    {
        return !left.Equals(right);
    }
}
