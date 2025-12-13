using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintEditor.Models;

public sealed class ModData
{
    private const string VersionKey = "version";
    private const string ModNameKey = "name";
    private const string SupportedVersionKey = "supported_version";

    public string SupportedVersion { get; set; } = string.Empty;
    public string ModName { get; set; } = "新建 mod";

    public void ParseScript(string filePath)
    {
        if (!TextParser.TryParse(filePath, out var node, out var error))
        {
            throw new ArgumentException(error.ErrorMessage);
        }
        string? version = node.GetLeaf(VersionKey)?.Value.ValueText;
        ArgumentNullException.ThrowIfNull(version);
        string? modName = node.GetLeaf(ModNameKey)?.Value.ValueText;
        ArgumentNullException.ThrowIfNull(modName);
        string? supportedVersion = node.GetLeaf(SupportedVersionKey)?.Value.ValueText;
        ArgumentNullException.ThrowIfNull(supportedVersion);

        SupportedVersion = supportedVersion;
        ModName = modName;
    }

    public string ToScript()
    {
        var node = Node.Create(string.Empty);
        node.AddLeafString(VersionKey, SupportedVersion);
        node.AddLeafString(ModNameKey, ModName);
        node.AddLeafString(SupportedVersionKey, SupportedVersion);
        return node.ToScript();
    }
}
