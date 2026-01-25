using System.Diagnostics;
using Avalonia.Media;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class FontIcon : FluentAvalonia.UI.Controls.FontIcon
{
    public FontIcon()
    {
        var fontFamily = (FontFamily?)App.Current.Resources["FluentIconFontFamily"];
        FontFamily = fontFamily ?? new FontFamily("Segoe Fluent Icons");

        Debug.Assert(FontFamily is not null);
    }
}
