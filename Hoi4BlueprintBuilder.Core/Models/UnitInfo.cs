using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Models;

public sealed class UnitInfo(
    string name,
    string group,
    bool isRegimental,
    bool isDivisional,
    int width,
    bool canBeParachuted,
    int manpower,
    bool allowInNonArmyHq,
    IReadOnlySet<string> allowedBattalionGroups,
    IReadOnlyCollection<(string Name, int Quantity)> equipments,
    UnitIntrinsicStats stats,
    IReadOnlyCollection<Child> modifiers
)
{
    public string Name { get; } = name;
    public string Group { get; } = group;

    /// <summary>
    /// 是团级支援营
    /// </summary>
    public bool IsRegimental { get; } = isRegimental;

    /// <summary>
    /// 是师级支援营
    /// </summary>
    public bool IsDivisional { get; } = isDivisional;

    /// <summary>
    /// 可以伞降
    /// </summary>
    public bool CanBeParachuted { get; } = canBeParachuted;

    /// <summary>
    /// 编制宽度
    /// </summary>
    public int Width { get; } = width;

    public int Manpower { get; } = manpower;

    /// <summary>
    /// 允许在非陆军指挥部中使用
    /// </summary>
    public bool AllowInNonArmyHq { get; } = allowInNonArmyHq;

    /// <summary>
    /// 需要的武器装备
    /// </summary>
    public IReadOnlyCollection<(string Name, int Quantity)> Equipments { get; } = equipments;

    public UnitIntrinsicStats Stats { get; } = stats;

    public IReadOnlyCollection<Child> Modifiers { get; } = modifiers;

    private IReadOnlySet<string> AllowedBattalionGroups { get; } = allowedBattalionGroups;

    public bool IsAllowedBattalionGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            return false;
        }

        if (AllowedBattalionGroups.Count == 0)
        {
            return true;
        }

        return AllowedBattalionGroups.Contains(groupName);
    }

    public string IconKey => $"GFX_unit_{Name}_icon_medium";
}

public sealed record UnitIntrinsicStats(
    double MaxStrength = 0,
    double MaxOrganisation = 0,
    double DefaultMorale = 0,
    double Recon = 0,
    double Suppression = 0,
    double Weight = 0,
    double SuppressionFactor = 0,
    double SupplyConsumption = 0,
    double CasualtyTrickleback = 0,
    double ExperienceLossFactor = 0,
    double EquipmentCaptureFactor = 0,
    double TrainingTime = 0,
    double Initiative = 0,
    // 堑壕值
    double Entrenchment = 0
);
