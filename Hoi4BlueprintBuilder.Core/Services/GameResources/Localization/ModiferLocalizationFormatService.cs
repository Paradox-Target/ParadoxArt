using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;

/// <summary>
/// 用来补全缺少的修饰符格式
/// </summary>
[RegisterSingleton<ModiferLocalizationFormatService>]
public sealed class ModiferLocalizationFormatService
{
    private readonly FrozenDictionary<string, string> _modifierLocalizationFormat;

    private const string FileName = "ModifierLocalizationFormatInfo.csv";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ModiferLocalizationFormatService()
        : this(AssetLoadHelper.OpenReadText(FileName)) { }

    public ModiferLocalizationFormatService(TextReader reader)
    {
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var formatMap = new Dictionary<string, string>(8);
        foreach (var record in csv.GetRecords<ModifierLocalizationFormatInfo>())
        {
            if (string.IsNullOrWhiteSpace(record.Key) || string.IsNullOrWhiteSpace(record.FormatInfo))
            {
#if DEBUG
                throw new ArgumentException("csv中有值为空");
#endif
                continue;
            }

            formatMap.Add(record.Key.Trim(), record.FormatInfo.Trim());
        }

        _modifierLocalizationFormat = formatMap.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetLocalizationFormat(string modifier, [NotNullWhen(true)] out string? formatInfo)
    {
        if (_modifierLocalizationFormat.TryGetValue(modifier, out formatInfo))
        {
            return true;
        }

        formatInfo = null;
        return false;
    }
}
