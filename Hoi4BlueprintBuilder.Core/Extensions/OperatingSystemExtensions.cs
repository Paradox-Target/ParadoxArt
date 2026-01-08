namespace Hoi4BlueprintBuilder.Core.Extensions;

public static class OperatingSystemExtensions
{
    extension(OperatingSystem)
    {
        /// <summary>
        /// 当是桌面设备(Windows, Linux, MacOS)时返回<c>true</c>, 否则返回<c>false</c>
        /// </summary>
        public static bool IsDesktop =>
            OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

        /// <summary>
        /// 当是移动设备(Android, IOS)时返回<c>true</c>, 否则返回<c>false</c>
        /// </summary>
        public static bool IsMobile => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
    }
}