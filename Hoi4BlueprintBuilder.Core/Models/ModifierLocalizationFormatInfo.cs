using CsvHelper;

namespace Hoi4BlueprintBuilder.Core.Models;

[CsvRecord]
public sealed partial class ModifierLocalizationFormatInfo(
    [CsvColumn("Key")] string key,
    [CsvColumn("Format Info")] string formatInfo
)
{
    public string Key { get; } = key;

    public string FormatInfo { get; } = formatInfo;
}
