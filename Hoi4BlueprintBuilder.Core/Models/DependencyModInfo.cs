using System.Text.Json.Serialization;

namespace Hoi4BlueprintBuilder.Core.Models;

[method: JsonConstructor]
public sealed class DependencyModInfo(string name, string rootDirectory)
{
    public string Name { get; } = name;

    public string RootDirectory { get; } = rootDirectory;
}
