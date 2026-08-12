using System.Collections.Frozen;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using MethodTimer;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources;

/// <summary>
/// 地形资源服务, 在 common/terrain 目录下
/// </summary>
[RegisterSingleton<TerrainService>]
public sealed class TerrainService : CommonResourcesService<TerrainService, List<TerrainDefinition>>
{
    /// <summary>
    /// 未在文件中定义的地形
    /// </summary>
    private static readonly string[] SpecialEnvironments = ["river", "fort", "amphibious", "snow"];

    private Lazy<IReadOnlyList<string>> _landTerrains;
    private Lazy<FrozenSet<string>> _allTerrainNames;

    private ICollection<List<TerrainDefinition>> AllTerrains => Resources.Values;

    [Time("加载地形资源")]
    public TerrainService(IServiceProvider serviceProvider)
        : base(
            Path.Combine(Keywords.Common, "terrain"),
            WatcherFilter.Text,
            serviceProvider,
            PathType.Folder,
            SearchOption.AllDirectories
        )
    {
        _landTerrains = new Lazy<IReadOnlyList<string>>(BuildLandTerrains);
        _allTerrainNames = new Lazy<FrozenSet<string>>(BuildAllTerrainNames);
        OnResourceChanged += (_, _) =>
        {
            _landTerrains = new Lazy<IReadOnlyList<string>>(BuildLandTerrains);
            _allTerrainNames = new Lazy<FrozenSet<string>>(BuildAllTerrainNames);
        };
    }

    public IReadOnlyList<string> LandTerrains => _landTerrains.Value;

    public bool Contains(string terrainName)
    {
        return _allTerrainNames.Value.Contains(terrainName);
    }

    private IReadOnlyList<string> BuildLandTerrains()
    {
        var list = new List<string>(8);
        foreach (var terrain in AllTerrains.SelectMany(static t => t))
        {
            if (terrain.IsWater || terrain.Name.EqualsIgnoreCase("unknown"))
            {
                continue;
            }

            list.Add(terrain.Name);
        }

        foreach (string se in SpecialEnvironments)
        {
            list.Add(se);
        }

        return list.ToArray();
    }

    private FrozenSet<string> BuildAllTerrainNames()
    {
        var allNames = new HashSet<string>(SpecialEnvironments, StringComparer.OrdinalIgnoreCase);
        foreach (var terrain in AllTerrains.SelectMany(static t => t))
        {
            allNames.Add(terrain.Name);
        }

        return allNames.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    protected override List<TerrainDefinition>? ParseFileToContent(Node rootNode)
    {
        foreach (var child in rootNode.AllArray)
        {
            if (!child.TryGetNode(out var node))
            {
                continue;
            }

            if (node.Key.EqualsIgnoreCase("categories"))
            {
                var terrains = new List<TerrainDefinition>(8);
                foreach (var terrainCategory in node.NodesValue)
                {
                    bool isWater = terrainCategory.LeavesValue.Any(leaf =>
                        (leaf.Key.EqualsIgnoreCase("is_water") || leaf.Key.EqualsIgnoreCase("naval_terrain"))
                        && leaf.Value.TryGetBool(out bool value)
                        && value
                    );
                    terrains.Add(new TerrainDefinition(terrainCategory.Key, isWater));
                }
                return terrains;
            }
        }

        return null;
    }
}

public readonly record struct TerrainDefinition(string Name, bool IsWater);
