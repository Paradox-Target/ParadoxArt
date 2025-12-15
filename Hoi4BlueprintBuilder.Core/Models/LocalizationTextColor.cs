using System.Drawing;

namespace Hoi4BlueprintBuilder.Core.Models;

public sealed class LocalizationTextColor(char key, Color color)
{
    public char Key { get; } = key;
    public Color Color { get; } = color;
}
