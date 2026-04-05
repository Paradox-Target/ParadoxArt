using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<GameModDescriptorService>]
public sealed class GameModDescriptorService
{
    public string Name { get; private set; } = string.Empty;
    public string[] DependenciesName { get; private set; } = [];
    public string SteamWorkshopId { get; private set; } = string.Empty;

    /// <summary>
    /// 保存着替换的文件夹相对路径的只读集合
    /// </summary>
    /// <remarks>
    /// 线程安全
    /// </remarks>
    public IReadOnlySet<string> ReplacePaths => _replacePaths;
    private FrozenSet<string> _replacePaths;

    private readonly SettingsService _settingService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 按Mod根目录查找描述文件
    /// </summary>
    public GameModDescriptorService(SettingsService settingService)
    {
        _settingService = settingService;
        Initialize();
    }

    [MemberNotNull(nameof(_replacePaths))]
    private void Initialize()
    {
        string descriptorFilePath = Path.Combine(
            _settingService.ModRootFolderPath,
            GameConstants.ModDescriptorFileName
        );
        if (!File.Exists(descriptorFilePath))
        {
            _replacePaths = [];
            Log.Warn("Mod 描述文件不存在");
            return;
        }

        if (!TextParser.TryParse(descriptorFilePath, out var rootNode, out var error))
        {
            _replacePaths = [];
            Log.Warn("Mod descriptor.mod file read is failure");
            Log.LogParseError(error);
            return;
        }

        var replacePathList = new List<string>();

        foreach (var child in rootNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                switch (leaf.Key)
                {
                    case "name":
                        Name = leaf.ValueText;
                        break;
                    case "replace_path":
                        string[] parts = leaf.ValueText.Split('/');
                        replacePathList.Add(Path.Combine(parts));
                        break;
                    case "remote_file_id":
                        SteamWorkshopId = leaf.ValueText;
                        break;
                }
            }
            else if (child.TryGetNode(out var node) && node.Key.EqualsIgnoreCase("dependencies"))
            {
                DependenciesName = node.LeafValuesValue.Select(static value => value.Key).ToArray();
            }
        }
        _replacePaths = replacePathList.ToFrozenSet(PlatformHelper.Comparer);
    }

    public void Reload()
    {
        Initialize();
    }
}
