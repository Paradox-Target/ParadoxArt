using System.Collections.Frozen;
using Hoi4BlueprintEditor.Extensions;
using NLog;
using ParadoxPower.CSharpExtensions;

namespace Hoi4BlueprintEditor.Services;

public sealed class GameModDescriptorService
{
    public string Name { get; } = string.Empty;

    /// <summary>
    /// 保存着替换的文件夹相对路径的只读集合
    /// </summary>
    /// <remarks>
    /// 线程安全
    /// </remarks>
    public IReadOnlySet<string> ReplacePaths => _replacePaths;
    private readonly FrozenSet<string> _replacePaths;

    private const string FileName = "descriptor.mod";

    /// <summary>
    /// 按Mod根目录查找描述文件
    /// </summary>
    public GameModDescriptorService(SettingsService settingService)
    {
        var logger = LogManager.GetCurrentClassLogger();
        string descriptorFilePath = Path.Combine(settingService.ModRootFolderPath, FileName);
        if (!File.Exists(descriptorFilePath))
        {
            _replacePaths = [];
            logger.Warn("Mod 描述文件不存在");
            return;
        }

        if (!TextParser.TryParse(descriptorFilePath, out var rootNode, out var error))
        {
            _replacePaths = [];
            logger.Warn("Mod descriptor.mod file read is failure");
            logger.LogParseError(error);
            return;
        }

        var replacePathList = new List<string>();

        foreach (var item in rootNode.Leaves)
        {
            switch (item.Key)
            {
                case "name":
                    Name = item.ValueText;
                    break;
                case "replace_path":
                    string[] parts = item.ValueText.Split('/');
                    replacePathList.Add(Path.Combine(parts));
                    break;
            }
        }
        _replacePaths = replacePathList.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
