using System.Diagnostics;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class FontIcon : FAFontIcon
{
    public FontIcon()
    {
        var fontFamily = (FontFamily?)App.Current.Resources["FluentIconFontFamily"];
        FontFamily = fontFamily ?? new FontFamily("Segoe Fluent Icons");

        Debug.Assert(FontFamily is not null);
    }
}
