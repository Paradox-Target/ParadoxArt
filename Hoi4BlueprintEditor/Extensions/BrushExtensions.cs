using System.Windows.Media;

namespace Hoi4BlueprintEditor.Extensions;

public static class BrushExtensions
{
    private static readonly BrushConverter _converter = new BrushConverter();

    /// <summary>
    /// 将十六进制颜色字符串 (例如 "#FF00FF" 或 "Red") 转换为 SolidColorBrush
    /// </summary>
    /// <param name="hexColorString">颜色字符串</param>
    /// <returns>一个 SolidColorBrush 对象</returns>

    public static SolidColorBrush? ToBrush(this string hexColorString)
    {
        try
        {
            return _converter.ConvertFromString(hexColorString) as SolidColorBrush;
        }
        catch
        {
            return null;
        }
    }
}
