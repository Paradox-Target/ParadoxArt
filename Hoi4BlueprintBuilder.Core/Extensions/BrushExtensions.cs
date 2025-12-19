using Avalonia.Media;

namespace Hoi4BlueprintBuilder.Core.Extensions;

public static class BrushExtensions
{
    private static readonly BrushConverter Converter = new();

    /// <summary>
    /// 将十六进制颜色字符串 (例如 "#FF00FF" 或 "Red") 转换为 SolidColorBrush
    /// </summary>
    /// <param name="hexColorString">颜色字符串</param>
    /// <returns>一个 SolidColorBrush 对象</returns>

    public static IBrush? ToBrush(this string hexColorString)
    {
        try
        {
            return Converter.ConvertFromString(hexColorString) as IBrush;
        }
        catch
        {
            return null;
        }
    }
}
