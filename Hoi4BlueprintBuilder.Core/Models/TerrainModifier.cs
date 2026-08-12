namespace Hoi4BlueprintBuilder.Core.Models;

public readonly record struct TerrainModifier(double Attack, double Movement, double Defence)
{
    private const double ZeroTolerance = 1e-12;

    public static TerrainModifier operator +(TerrainModifier left, TerrainModifier right) =>
        new(left.Attack + right.Attack, left.Movement + right.Movement, left.Defence + right.Defence);

    public static TerrainModifier operator /(TerrainModifier value, double divisor) =>
        new(value.Attack / divisor, value.Movement / divisor, value.Defence / divisor);

    public TerrainModifier Normalize() =>
        new(NormalizeValue(Attack), NormalizeValue(Movement), NormalizeValue(Defence));

    public bool IsZero => IsNearlyZero(Attack) && IsNearlyZero(Movement) && IsNearlyZero(Defence);

    private static double NormalizeValue(double value) => IsNearlyZero(value) ? 0 : value;

    private static bool IsNearlyZero(double value) => Math.Abs(value) < ZeroTolerance;
}

public readonly record struct TerrainModifierResult(string Terrain, TerrainModifier Modifier);
