using CsvHelper;

namespace Hoi4BlueprintBuilder.Core.Models;

[CsvRecord]
public sealed partial class LocalizationKeyMappingInfo(
    [CsvColumn("Raw Key")] string rawKey,
    [CsvColumn("Mapping Key")] string mappingKey
)
{
    public string RawKey { get; } = rawKey;

    public string MappingKey { get; } = mappingKey;
}
