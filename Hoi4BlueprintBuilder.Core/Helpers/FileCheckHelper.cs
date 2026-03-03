using System.Collections.Frozen;
using DotNet.Globbing;

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

    private static readonly Glob FocusGlob = Glob.Parse(
        "**/common/national_focus/*.txt",
        new GlobOptions { Evaluation = new EvaluationOptions { CaseInsensitive = false } }
    );

    public static bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath.AsSpan());
        return TextExtensionNamesLookup.Contains(extension);
    }

    public static bool IsFocusTreeFile(string filePath)
    {
        return FocusGlob.IsMatch(filePath);
    }
}
