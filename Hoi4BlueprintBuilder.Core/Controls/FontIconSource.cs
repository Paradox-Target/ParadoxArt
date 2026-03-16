using System.Diagnostics;
using Avalonia.Media;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class FontIconSource : FluentAvalonia.UI.Controls.FontIconSource
{
    public FontIconSource()
    {
        FontFamily? fontFamily;
        try
        {
            // 单元测试中无法获取到这个资源, 所以捕获一下异常
            fontFamily = (FontFamily?)App.Current.Resources["FluentIconFontFamily"];
        }
        catch (Exception)
        {
            fontFamily = null;
        }
        FontFamily = fontFamily ?? new FontFamily("Segoe Fluent Icons");

        Debug.Assert(FontFamily is not null);
    }
}
