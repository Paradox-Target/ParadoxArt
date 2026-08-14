using System.Globalization;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using ParadoxPower.CSharpExtensions;

namespace Hoi4BlueprintBuilder.UnitTests.Services.GameResources;

[TestFixture]
public sealed class TerrainModifierCalculatorTests
{
    [Test]
    public void Calculate_AveragesMissingLineTerrainValuesAsZero()
    {
        var first = CreateUnit("first", "forest", ("attack", 0.2));
        var second = CreateUnit("second", "plains", ("attack", 0.4));

        var result = CalculateModifier([first, second], [], [], "forest");

        Assert.That(result, Is.EqualTo(new TerrainModifier(0.1, 0, 0)));
    }

    [Test]
    public void Calculate_AddsDivisionalAndRegimentalSupportAtFullValue()
    {
        var line = CreateUnit("line", "forest", ("attack", 0.2));
        var divisional = CreateUnit("divisional", "forest", ("movement", 0.3));
        var regimental = CreateUnit("regimental", "forest", ("defence", -0.4));

        var result = CalculateModifier([line], [divisional], [regimental], "forest");

        Assert.That(result, Is.EqualTo(new TerrainModifier(0.2, 0.3, -0.4)));
    }

    [Test]
    public void Calculate_OmitsRowsOnlyAfterAllStatsCancel()
    {
        var first = CreateUnit("first", "forest", ("attack", 0.2), ("movement", -0.1));
        var second = CreateUnit("second", "forest", ("attack", -0.2), ("movement", 0.1));

        var result = new TerrainModifierCalculator().Calculate([first, second], [], [], ["forest", "plains"]);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Calculate_OmitsRowsWithOnlyFloatingPointRemainder()
    {
        var first = CreateUnit("first", "forest", ("attack", 0.1));
        var second = CreateUnit("second", "forest", ("attack", 0.2));
        var support = CreateUnit("support", "forest", ("attack", -0.15));

        var result = new TerrainModifierCalculator().Calculate([first, second], [support], [], ["forest"]);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Calculate_NormalizesFloatingPointRemainderInVisibleRow()
    {
        var first = CreateUnit("first", "forest", ("attack", 0.1));
        var second = CreateUnit("second", "forest", ("attack", 0.2));
        var support = CreateUnit("support", "forest", ("attack", -0.15), ("movement", 0.25));

        var result = CalculateModifier([first, second], [support], [], "forest");

        Assert.That(result, Is.EqualTo(new TerrainModifier(0, 0.25, 0)));
    }

    [Test]
    public void CanApplyRegimentalSupport_ReturnsFalseWhenBattalionGroupChanged()
    {
        var calculator = new TerrainModifierCalculator();
        var support = CreateEligibilityUnit("support", "armored");
        var infantry = CreateEligibilityUnit("infantry");

        bool result = calculator.CanApplyRegimentalSupport(support, [infantry], 1);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Calculate_IgnoresUnknownTerrainAndStats()
    {
        var unit = CreateUnit("line", "forest", ("attack", 0.2), ("unknown_stat", 99));

        var result = CalculateModifier([unit], [], [], "forest", "unknown");

        Assert.That(result, Is.EqualTo(new TerrainModifier(0.2, 0, 0)));
    }

    private static TerrainModifier CalculateModifier(
        IReadOnlyCollection<UnitInfo> line,
        IReadOnlyCollection<UnitInfo> divisional,
        IReadOnlyCollection<UnitInfo> regimental,
        params string[] terrain
    )
    {
        var results = new TerrainModifierCalculator().Calculate(line, divisional, regimental, terrain);
        return results.Count == 0 ? default : results[0].Modifier;
    }

    private static UnitInfo CreateUnit(
        string name,
        string terrain,
        params (string Stat, double Value)[] modifiers
    )
    {
        string body = string.Join(
            Environment.NewLine,
            modifiers.Select(modifier =>
                $"{modifier.Stat} = {modifier.Value.ToString(CultureInfo.InvariantCulture)}"
            )
        );
        TextParser.TryParse(string.Empty, $"{terrain} = {{ {body} }}", out var root, out var error);
        Assert.That(error, Is.Null);
        Assert.That(root, Is.Not.Null);

        return new UnitInfo(
            name,
            "infantry",
            true,
            true,
            1,
            true,
            0,
            true,
            new HashSet<string>(),
            [],
            new UnitIntrinsicStats(),
            new HashSet<string>(),
            [],
            root!.AllArray
        );
    }

    private static UnitInfo CreateEligibilityUnit(string group, params string[] allowedGroups) =>
        new(
            group,
            group,
            true,
            true,
            1,
            true,
            0,
            true,
            allowedGroups.ToHashSet(StringComparer.OrdinalIgnoreCase),
            [],
            new UnitIntrinsicStats(),
            new HashSet<string>(),
            [],
            []
        );
}
