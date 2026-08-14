using System.Collections.Frozen;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources;

[RegisterSingleton<UnitService>]
public sealed class UnitService : CommonResourcesService<UnitService, Dictionary<string, UnitInfo>>
{
    private static readonly FrozenSet<string> NonModifiersKeys = new HashSet<string>
    {
        "affects_speed",
        "active",
        "ai_priority",
        "priority",
        "map_icon_category",
        "abbreviation",
        "sprite",
        "type",
        "group",
        "categories",
        "regimental",
        "divisional",
        "training_time",
        "same_support_type",
        "allowed_battalion_groups",
        "combat_width",
        "weight",
        // 不知道有什么用, 先过滤掉
        "essential",
        "max_strength",
        "max_organisation",
        "default_morale",
        "casualty_trickleback",
        "breakthrough",
        "soft_attack",
        "hard_attack",
        "defense"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public UnitService(IServiceProvider serviceProvider)
        : base(
            Path.Combine(Keywords.Common, "units"),
            WatcherFilter.Text,
            serviceProvider,
            PathType.Folder,
            SearchOption.AllDirectories,
            true
        ) { }

    public IEnumerable<UnitInfo> AllUnits => Resources.Values.SelectMany(map => map.Values);

    protected override Dictionary<string, UnitInfo> ParseFileToContent(Node rootNode)
    {
        var map = new Dictionary<string, UnitInfo>();

        foreach (var subUnitsNode in rootNode.NodesValue.Where(n => n.Key.EqualsIgnoreCase("sub_units")))
        {
            foreach (var unitNode in subUnitsNode.NodesValue)
            {
                ParseUnitInfo(unitNode, map);
            }
        }

        return map;
    }

    private void ParseUnitInfo(Node unitNode, Dictionary<string, UnitInfo> map)
    {
        string? groupName = null;
        bool isRegimental = true;
        bool isDivisional = true;
        int width = 0;
        bool canBeParachuted = false;
        int manpower = 0;
        bool allowInNonArmyHq = true;
        var allowedGroups = new HashSet<string>();
        var requirements = new List<(string Name, int Quantity)>();
        var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var battalionMultipliers = new List<BattalionMultiplier>();

        foreach (var child in unitNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                if (leaf.Key.EqualsIgnoreCase("group"))
                {
                    groupName = leaf.ValueText;
                }
                else if (
                    leaf.Key.EqualsIgnoreCase("regimental") && leaf.Value.TryGetBool(out bool regimental)
                )
                {
                    isRegimental = regimental;
                }
                else if (
                    leaf.Key.EqualsIgnoreCase("divisional") && leaf.Value.TryGetBool(out bool divisional)
                )
                {
                    isDivisional = divisional;
                }
                else if (leaf.Key.EqualsIgnoreCase("combat_width"))
                {
                    leaf.Value.TryGetInt(out width);
                }
                else if (
                    leaf.Key.EqualsIgnoreCase("can_be_parachuted")
                    && leaf.Value.TryGetBool(out bool parachuted)
                )
                {
                    canBeParachuted = parachuted;
                }
                else if (leaf.Key.EqualsIgnoreCase("manpower"))
                {
                    leaf.Value.TryGetInt(out manpower);
                }
                else if (
                    leaf.Key.EqualsIgnoreCase("allow_in_non_army_hq")
                    && leaf.Value.TryGetBool(out bool allowInNonArmyHqBool)
                )
                {
                    allowInNonArmyHq = allowInNonArmyHqBool;
                }
            }
            else if (child.TryGetNode(out var node))
            {
                if (node.Key.EqualsIgnoreCase("allowed_battalion_groups"))
                {
                    foreach (var group in node.LeafValuesValue)
                    {
                        allowedGroups.Add(group.Key);
                    }
                }
                else if (node.Key.EqualsIgnoreCase("categories"))
                {
                    foreach (var category in node.LeafValuesValue)
                    {
                        categories.Add(category.Key);
                    }
                }
                else if (node.Key.EqualsIgnoreCase("battalion_mult"))
                {
                    var multiplier = ParseBattalionMultiplier(node, unitNode.Key);
                    if (multiplier is not null)
                    {
                        battalionMultipliers.Add(multiplier);
                    }
                }
                else if (node.Key.EqualsIgnoreCase("need"))
                {
                    foreach (var requirement in node.LeavesValue)
                    {
                        if (requirement.Value.TryGetInt(out int quantity))
                        {
                            requirements.Add((requirement.Key, quantity));
                        }
                    }
                }
            }
        }
        if (groupName is null)
        {
            Log.Warn("{Name} 不存在分组信息", unitNode.Key);
            return;
        }
        allowedGroups.TrimExcess();
        categories.TrimExcess();
        map[unitNode.Key] = new UnitInfo(
            unitNode.Key,
            groupName,
            isRegimental,
            isDivisional,
            width,
            canBeParachuted,
            manpower,
            allowInNonArmyHq,
            allowedGroups,
            [.. requirements],
            ParseIntrinsicStats(unitNode),
            categories,
            battalionMultipliers,
            unitNode
                .AllArray.AsValueEnumerable()
                .Where(child =>
                {
                    string key;
                    if (child.TryGetLeaf(out var leaf))
                    {
                        key = leaf.Key;
                    }
                    else if (child.TryGetNode(out var node))
                    {
                        key = node.Key;
                    }
                    else
                    {
                        return false;
                    }

                    if (NonModifiersKeys.Contains(key))
                    {
                        return false;
                    }

                    return true;
                })
                .ToArray()
        );
    }

    private BattalionMultiplier? ParseBattalionMultiplier(Node node, string unitName)
    {
        string? category = null;
        bool isAdditive = false;
        bool foundCategory = false;
        bool foundAdd = false;

        foreach (var leaf in node.LeavesValue)
        {
            if (leaf.Key.EqualsIgnoreCase("category"))
            {
                category = leaf.ValueText;
                foundCategory = true;
            }
            else if (leaf.Key.EqualsIgnoreCase("add") && leaf.Value.TryGetBool(out bool add))
            {
                isAdditive = add;
                foundAdd = true;
            }

            if (foundAdd && foundCategory)
            {
                break;
            }
        }
        var modifiers = ParseIntrinsicStats(node);

        if (string.IsNullOrEmpty(category))
        {
            Log.Warn("{UnitName} 的 battalion_mult 缺少 category", unitName);
            return null;
        }

        return new BattalionMultiplier(category, modifiers, isAdditive);
    }

    private static UnitIntrinsicStats ParseIntrinsicStats(Node unitNode)
    {
        double maxStrength = 0;
        double maxOrganisation = 0;
        double defaultMorale = 0;
        double recon = 0;
        double suppression = 0;
        double suppressionFactor = 0;
        double supplyConsumption = 0;
        double casualtyTrickleback = 0;
        double experienceLossFactor = 0;
        double equipmentCaptureFactor = 0;
        double trainingTime = 0;
        double initiative = 0;
        double entrenchment = 0;
        double weight = 0;
        double supplyConsumptionFactor = 0;

        foreach (var leaf in unitNode.LeavesValue)
        {
            if (!leaf.Value.TryGetDouble(out double value))
            {
                continue;
            }

            if (leaf.Key.EqualsIgnoreCase("max_strength"))
            {
                maxStrength = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("max_organisation"))
            {
                maxOrganisation = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("default_morale"))
            {
                defaultMorale = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("recon"))
            {
                recon = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("suppression"))
            {
                suppression = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("suppression_factor"))
            {
                suppressionFactor = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("supply_consumption"))
            {
                supplyConsumption = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("casualty_trickleback"))
            {
                casualtyTrickleback = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("experience_loss_factor"))
            {
                experienceLossFactor = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("weight"))
            {
                weight = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("equipment_capture_factor"))
            {
                equipmentCaptureFactor = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("training_time"))
            {
                trainingTime = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("initiative"))
            {
                initiative = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("entrenchment"))
            {
                entrenchment = value;
            }
            else if (leaf.Key.EqualsIgnoreCase("supply_consumption_factor"))
            {
                supplyConsumptionFactor = value;
            }
        }

        return new UnitIntrinsicStats(
            maxStrength,
            maxOrganisation,
            defaultMorale,
            recon,
            suppression,
            weight,
            suppressionFactor,
            supplyConsumption,
            supplyConsumptionFactor,
            casualtyTrickleback,
            experienceLossFactor,
            equipmentCaptureFactor,
            trainingTime,
            initiative,
            entrenchment
        );
    }
}
