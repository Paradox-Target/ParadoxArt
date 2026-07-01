using System.Text.Json.Serialization;

namespace Hoi4BlueprintBuilder.Core.Models;

[method: JsonConstructor]
public sealed class IconFavorites(string name, List<string> icons)
{
    public string Name { get; } = name;
    public List<string> Icons { get; } = icons;
}
