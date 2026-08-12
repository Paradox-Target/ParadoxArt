using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;

/// <summary>
/// 用来解决脚本关键字与本地化文本中的键不一致的问题
/// </summary>
[RegisterSingleton<LocalizationKeyMappingService>]
public sealed class LocalizationKeyMappingService
{
    /// <summary>
    /// 当调用方法查找Key对应的本地化文本时,如果字典内存在Key, 则使用Key对应的Value进行查询
    /// </summary>
    private readonly FrozenDictionary<string, string> _localisationKeyMapping;

    private const string FileName = "ModiferLocalizationKeyMapping.csv";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public LocalizationKeyMappingService()
        : this(AssetLoadHelper.OpenReadText(FileName)) { }

    public LocalizationKeyMappingService(TextReader reader)
    {
        // 方便贡献, 冲突时可以处理, 因此不应该是二进制(可以生成二进制缓存文件, 第一次启动时生成二进制文件, 并记录Hash, 当更新时重新生成)
        // TODO: 使用二进制? 因为内嵌了.

        var localisationKeyMapping = new Dictionary<string, string>(16);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        foreach (var info in csv.GetRecords<LocalizationKeyMappingInfo>())
        {
            if (string.IsNullOrWhiteSpace(info.RawKey) || string.IsNullOrWhiteSpace(info.MappingKey))
            {
#if DEBUG
                throw new ArgumentException("csv中有值为空");
#endif
                continue;
            }

            var rawKey = info.RawKey.Trim();
            var mappingKey = info.MappingKey.Trim();
            localisationKeyMapping[rawKey] = mappingKey;
        }

        _localisationKeyMapping = localisationKeyMapping.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        Log.Info("成功加载本地化键映射表");
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? mappingKey)
    {
        return _localisationKeyMapping.TryGetValue(key, out mappingKey);
    }
}
