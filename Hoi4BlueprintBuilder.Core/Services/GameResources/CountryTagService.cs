using System.Collections.Frozen;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources;

[RegisterSingleton<CountryTagService>]
public sealed class CountryTagService : CommonResourcesService<CountryTagService, FrozenSet<string>>
{
    /// <summary>
    /// 在游戏内注册的国家标签
    /// </summary>
    public IReadOnlyCollection<string> CountryTags => _countryTagsLazy.Value;

    private Lazy<IReadOnlyCollection<string>> _countryTagsLazy;

    public CountryTagService(IServiceProvider serviceProvider)
        : base(Path.Combine(Keywords.Common, "country_tags"), WatcherFilter.Text, serviceProvider)
    {
        _countryTagsLazy = new Lazy<IReadOnlyCollection<string>>(GetCountryTags);
        OnResourceChanged += (_, _) =>
        {
            _countryTagsLazy = new Lazy<IReadOnlyCollection<string>>(GetCountryTags);
            Log.Debug("Country tags changed, 已重置");
        };
    }

    private string[] GetCountryTags()
    {
        return Resources.Values.AsValueEnumerable().SelectMany(set => set.Items).ToArray();
    }

    public bool Contains(string countryTag)
    {
        foreach (var countryTags in Resources.Values)
        {
            if (countryTags.Contains(countryTag))
            {
                return true;
            }
        }

        return false;
    }

    protected override FrozenSet<string>? ParseFileToContent(Node rootNode)
    {
        var leaves = rootNode.Leaves.ToArray();
        // 不加载临时标签
        if (
            leaves
                .AsValueEnumerable()
                .Any(leaf =>
                    leaf.Key.EqualsIgnoreCase("dynamic_tags")
                    && leaf.Value.TryGetBool(out bool isDynamicTags)
                    && isDynamicTags
                )
        )
        {
            return null;
        }

        var countryTags = new HashSet<string>(leaves.Length);
        foreach (var leaf in leaves)
        {
            string countryTag = leaf.Key;
            // 国家标签长度必须为 3
            if (countryTag.Length != 3)
            {
                continue;
            }
            countryTags.Add(countryTag);
        }
        return countryTags.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
