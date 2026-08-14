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
    IReadOnlySet<string> categories,
    IReadOnlyCollection<BattalionMultiplier> battalionMultipliers,
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

    /// <summary>
    /// 单位所属的 category
    /// </summary>
    private IReadOnlySet<string> Categories { get; } = categories;

    /// <summary>
    /// 单位拥有的 <c>battalion_mult</c> 效果
    /// </summary>
    public IReadOnlyCollection<BattalionMultiplier> BattalionMultipliers { get; } = battalionMultipliers;

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

    public bool IsInCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return false;
        }

        return Categories.Contains(category);
    }

    public string IconKey => $"GFX_unit_{Name}_icon_medium";
}

/// <summary>
/// 单位对指定 category 的营产生的属性叠加效果
/// </summary>
public sealed record BattalionMultiplier(
    string Category,
    UnitIntrinsicStats Stats,
    /// <summary>
    /// 为 <c>true</c> 时直接加数值; 为 <c>false</c> 时按目标当前值的百分比叠加.
    /// </summary>
    bool IsAdditive = false
);

public sealed record UnitIntrinsicStats(
    double MaxStrength = 0,
    double MaxOrganisation = 0,
    double DefaultMorale = 0,
    double Recon = 0,
    double Suppression = 0,
    double Weight = 0,
    double SuppressionFactor = 0,
    double SupplyConsumption = 0,
    double SupplyConsumptionFactor = 0,
    double CasualtyTrickleback = 0,
    double ExperienceLossFactor = 0,
    double EquipmentCaptureFactor = 0,
    double TrainingTime = 0,
    double Initiative = 0,
    // 堑壕值
    double Entrenchment = 0
)
{
    /// <summary>
    /// 叠加另一组单位固有属性; 训练时间取较大值.
    /// </summary>
    public UnitIntrinsicStats Add(UnitIntrinsicStats other) =>
        new(
            MaxStrength + other.MaxStrength,
            MaxOrganisation + other.MaxOrganisation,
            DefaultMorale + other.DefaultMorale,
            Recon + other.Recon,
            Suppression + other.Suppression,
            Weight + other.Weight,
            SuppressionFactor + other.SuppressionFactor,
            SupplyConsumption + other.SupplyConsumption,
            SupplyConsumptionFactor + other.SupplyConsumptionFactor,
            CasualtyTrickleback + other.CasualtyTrickleback,
            ExperienceLossFactor + other.ExperienceLossFactor,
            EquipmentCaptureFactor + other.EquipmentCaptureFactor,
            Math.Max(TrainingTime, other.TrainingTime),
            Initiative + other.Initiative,
            Entrenchment + other.Entrenchment
        );

    public UnitIntrinsicStats Multiply(UnitIntrinsicStats other)
    {
        return new UnitIntrinsicStats(
            MaxStrength * (1 + other.MaxStrength),
            MaxOrganisation * (1 + other.MaxOrganisation),
            DefaultMorale * (1 + other.DefaultMorale),
            Recon * (1 + other.Recon),
            Suppression * (1 + other.Suppression),
            Weight * (1 + other.Weight),
            SuppressionFactor * (1 + other.SuppressionFactor),
            SupplyConsumption * (1 + other.SupplyConsumption),
            SupplyConsumptionFactor * (1 + other.SupplyConsumptionFactor),
            CasualtyTrickleback * (1 + other.CasualtyTrickleback),
            ExperienceLossFactor * (1 + other.ExperienceLossFactor),
            EquipmentCaptureFactor * (1 + other.EquipmentCaptureFactor),
            TrainingTime * (1 + other.TrainingTime),
            Initiative * (1 + other.Initiative),
            Entrenchment * (1 + other.Entrenchment)
        );
    }
}
