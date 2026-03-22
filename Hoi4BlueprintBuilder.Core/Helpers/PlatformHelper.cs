namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class PlatformHelper
{
    /// <summary>
    /// 获取适用于当前平台的文件系统路径比较器
    /// (Windows 和 macOS 默认不敏感，Linux 敏感)
    /// </summary>
    public static StringComparer Comparer =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    /// <summary>
    /// 获取适用于当前平台的文件系统路径比较规则
    /// </summary>
    public static StringComparison Comparison =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
