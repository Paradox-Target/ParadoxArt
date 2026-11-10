using System.Collections.Frozen;
using DotNet.Globbing;
using Hoi4BlueprintBuilder.Core.Extensions;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class FileCheckHelper
{
    /// <summary>
    /// 获取当前平台配置的游戏可执行文件名称列表
    /// </summary>
    public static IReadOnlyCollection<string> GameExeFileNames => GetGameExeFileNames();

    private static IReadOnlyCollection<string> GetGameExeFileNames()
    {
        if (OperatingSystem.IsWindows())
        {
            return ["hoi4.exe"];
        }

        if (OperatingSystem.IsLinux())
        {
            return ["hoi4"];
        }

        if (OperatingSystem.IsMobile)
        {
            return ["hoi4.exe", "hoi4"];
        }

        throw new PlatformNotSupportedException("当前平台不存在配置的游戏可执行文件名称");
    }

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

    private static readonly Glob FocusGlob = Glob.Parse("**/common/national_focus/*.txt");

    private static readonly Glob OobUnitsGlob = Glob.Parse("**/history/units/*.txt");

    public static bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath.AsSpan());
        return TextExtensionNamesLookup.Contains(extension);
    }

    public static bool IsFocusTreeFile(string filePath)
    {
        return FocusGlob.IsMatch(filePath);
    }

    /// <summary>
    /// 判断文件是否位于 <c>history/units</c> 目录下的部队模板文件
    /// </summary>
    public static bool IsOobUnitsFile(string filePath)
    {
        return OobUnitsGlob.IsMatch(filePath);
    }

    public static bool IsValidGameRootDirectory(string directoryPath)
    {
        var names = GameExeFileNames;
        foreach (string name in names)
        {
            bool isExist = File.Exists(Path.Combine(directoryPath, name));
            if (isExist)
            {
                return true;
            }
        }

        return false;
    }
}
