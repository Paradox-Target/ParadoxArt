using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Hoi4BlueprintBuilder.Core.Services;

[JsonSerializable(typeof(ProjectConfigService))]
internal partial class ProjectConfigServiceContext : JsonSerializerContext;

public sealed class ProjectConfigService : BaseSettingsService<ProjectConfigService>, IDisposable
{
    /// <summary>
    /// 关闭程序前打开的文件的相对路径
    /// </summary>
    public List<string> OpenedFiles { get; set; } = [];

    /// <summary>
    /// 关闭程序前展开的文件夹的相对路径
    /// </summary>
    public List<string> ExpandedFolders { get; set; } = [];

    private SettingsService? _settingsService;

    private const string ConfigFileName = "project.json";

    protected override JsonTypeInfo<ProjectConfigService> JsonTypeInfo =>
        ProjectConfigServiceContext.Default.ProjectConfigService;

    public static ProjectConfigService Load(SettingsService settingsService)
    {
        string filePath = GetFilePath(settingsService.ModRootFolderPath);

        var service = LoadInternal(filePath, ProjectConfigServiceContext.Default.ProjectConfigService);
        service._settingsService = settingsService;
        return service;
    }

    private static string GetFilePath(string? modRootFolderPath)
    {
        if (string.IsNullOrWhiteSpace(modRootFolderPath))
        {
            throw new ArgumentNullException(nameof(modRootFolderPath));
        }

        return Path.Combine(modRootFolderPath, App.ProjectConfigDirectoryName, ConfigFileName);
    }

    public override void SaveSettings()
    {
        if (string.IsNullOrWhiteSpace(_settingsService?.ModRootFolderPath))
        {
            Log.Warn("未指定模组根目录，无法保存项目配置");
            return;
        }

        base.SaveSettings();
    }

    public void Dispose()
    {
        SaveSettings();
    }
}
