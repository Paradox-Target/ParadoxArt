using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Services.GameResources.Base;
using MethodTimer;
using ParadoxPower.CSharp;
using ParadoxPower.Localisation;

namespace Hoi4BlueprintEditor.Services.GameResources.Localization;

public sealed class LocalizationService
    : ResourcesService<LocalizationService, FrozenDictionary<string, string>, YAMLLocalisationParser.LocFile>
{
    private ICollection<FrozenDictionary<string, string>> Localisations => Resources.Values;

    [Time("加载本地化文件")]
    public LocalizationService()
        : base(
            // TODO: 按项目设置
            Path.Combine("localisation", "simp_chinese"),
            WatcherFilter.LocalizationFiles,
            PathType.Folder,
            SearchOption.AllDirectories,
            true
        ) { }

    /// <summary>
    /// 如果本地化文本不存在, 则返回<c>key</c>
    /// </summary>
    /// <returns></returns>
    public string GetValue(string key)
    {
        return TryGetValue(key, out string? value) ? value : key;
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        foreach (var localisation in Localisations)
        {
            if (localisation.TryGetValue(key, out string? result))
            {
                value = result;
                return true;
            }
        }

        value = null;
        return false;
    }

    protected override FrozenDictionary<string, string> ParseFileToContent(
        YAMLLocalisationParser.LocFile result
    )
    {
        var localisations = new Dictionary<string, string>(result.Entries.Count);
        foreach (var item in result.Entries)
        {
            localisations[item.Key] = item.Desc;
        }

        return localisations.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    protected override YAMLLocalisationParser.LocFile? GetParseResult(string filePath)
    {
        var localisation = YAMLLocalisationParser.ParseLocFile(filePath);
        if (localisation.IsFailure)
        {
            Log.LogParseError(localisation.GetError()!);
            return null;
        }

        var result = localisation.GetResult()!;
        return result;
    }
}
