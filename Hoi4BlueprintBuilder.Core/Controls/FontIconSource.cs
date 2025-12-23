using System.Diagnostics;
using Avalonia.Media;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class FontIconSource : FluentAvalonia.UI.Controls.FontIconSource
{
    public FontIconSource()
    {
        var fontFamily = (FontFamily?)App.Current.Resources["FluentIconFontFamily"];
        FontFamily = fontFamily ?? new FontFamily("Segoe Fluent Icons");

        Debug.Assert(FontFamily is not null);
    }
}
