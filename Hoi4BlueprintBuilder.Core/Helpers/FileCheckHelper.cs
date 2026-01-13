using System.Collections.Frozen;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class FileCheckHelper
{
    private static readonly FrozenSet<string> TextExtensionNames = new HashSet<string>
    {
        ".txt",
        ".md",
        ".json",
        ".mod",
        ".gui",
        ".gfx",
        ".lua",
        ".yml",
        ".py",
        ".ini"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> TextExtensionNamesLookup =
        TextExtensionNames.GetAlternateLookup<ReadOnlySpan<char>>();

    public static bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath.AsSpan());
        return TextExtensionNamesLookup.Contains(extension);
    }
}
