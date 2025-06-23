using System.Drawing;

namespace Hoi4BlueprintEditor.Models;

public sealed class TextFormatInfo(string text, Color color)
{
    public string DisplayText { get; } = text;
    public Color Color { get; } = color;
}
