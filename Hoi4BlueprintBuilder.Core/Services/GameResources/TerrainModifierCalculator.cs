using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Models;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources;

[RegisterSingleton<TerrainModifierCalculator>]
public sealed class TerrainModifierCalculator
{
    public IReadOnlyList<TerrainModifierResult> Calculate(
        IReadOnlyCollection<UnitInfo> lineBattalions,
        IReadOnlyCollection<UnitInfo> divisionalSupport,
        IReadOnlyCollection<UnitInfo> regimentalSupport,
        IEnumerable<string> terrainOrder
    )
    {
        var results = new List<TerrainModifierResult>();
        var seenTerrains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string terrain in terrainOrder)
        {
            if (string.IsNullOrWhiteSpace(terrain) || !seenTerrains.Add(terrain))
            {
                continue;
            }

            TerrainModifier modifier = default;
            foreach (var unit in lineBattalions)
            {
                modifier += GetModifier(unit.Modifiers, terrain);
            }

            if (lineBattalions.Count > 0)
            {
                modifier /= lineBattalions.Count;
            }

            foreach (var unit in divisionalSupport)
            {
                modifier += GetModifier(unit.Modifiers, terrain);
            }

            foreach (var unit in regimentalSupport)
            {
                modifier += GetModifier(unit.Modifiers, terrain);
            }

            modifier = modifier.Normalize();
            if (!modifier.IsZero)
            {
                results.Add(new TerrainModifierResult(terrain, modifier));
            }
        }

        return results;
    }

    public bool CanApplyRegimentalSupport(
        UnitInfo regimentalSupport,
        IReadOnlyCollection<UnitInfo> lineBattalions,
        int minimumBattalionCount
    ) =>
        lineBattalions.Count > 0
        && lineBattalions.Count >= minimumBattalionCount
        && lineBattalions
            .AsValueEnumerable()
            .All(unit => regimentalSupport.IsAllowedBattalionGroup(unit.Group));

    private static TerrainModifier GetModifier(IEnumerable<Child> modifiers, string terrain)
    {
        foreach (var child in modifiers)
        {
            if (!child.TryGetNode(out var node) || !node.Key.EqualsIgnoreCase(terrain))
            {
                continue;
            }

            double attack = 0;
            double movement = 0;
            double defence = 0;
            foreach (var leaf in node.LeavesValue)
            {
                if (!leaf.Value.TryGetDouble(out var value))
                {
                    continue;
                }

                if (leaf.Key.EqualsIgnoreCase("attack"))
                {
                    attack += value;
                }
                else if (leaf.Key.EqualsIgnoreCase("movement"))
                {
                    movement += value;
                }
                else if (leaf.Key.EqualsIgnoreCase("defence"))
                {
                    defence += value;
                }
            }

            return new TerrainModifier(attack, movement, defence);
        }

        return default;
    }
}
