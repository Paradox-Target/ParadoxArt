namespace Hoi4BlueprintEditor.Models.Focus;

public struct Point(int x, int y) : IEquatable<Point>
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    
    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public bool Equals(Point other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Point other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (X * 397) ^ Y;
        }
    }

    public static bool operator ==(Point left, Point right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Point left, Point right)
    {
        return !left.Equals(right);
    }
}