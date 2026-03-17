using System.Text.Json.Serialization;

namespace Hoi4BlueprintBuilder.Core.Models;

public sealed class ProjectItem : IEquatable<ProjectItem>
{
    [method: JsonConstructor]
    public ProjectItem(string name, string directoryPath)
    {
        Name = name;
        DirectoryPath = directoryPath;
        IsPathExist = Directory.Exists(DirectoryPath);
    }

    /// <summary>
    /// 项目名称, 一般为Mod名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Mod 文件夹路径
    /// </summary>
    public string DirectoryPath { get; }

    [JsonIgnore]
    public bool IsPathExist { get; }

    public bool Equals(ProjectItem? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name && DirectoryPath == other.DirectoryPath;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ProjectItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, DirectoryPath);
    }

    public static bool operator ==(ProjectItem? left, ProjectItem? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ProjectItem? left, ProjectItem? right)
    {
        return !Equals(left, right);
    }
}
