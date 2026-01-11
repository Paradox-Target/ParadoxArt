using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;

namespace Hoi4BlueprintBuilder.Core.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SettingsService))]
internal partial class SettingsServiceContext : JsonSerializerContext;

public sealed class SettingsService : BaseSettingsService<SettingsService>
{
    public string AppLanguage { get; set; } = string.Empty;
    public GameLanguage GameLanguage { get; set; } = LanguageHelper.GetGameLanguageBySystemLanguage();
    public string GameRootFolderPath { get; set; } = string.Empty;
    public string ModRootFolderPath { get; set; } = string.Empty;
    public ThemeMode ThemeMode { get; set; } = ThemeMode.Default;

    /// <summary>
    /// 导入国策图片时是否自动将 png 转换为 dds 格式
    /// </summary>
    public bool IsAutoFocusPngConvertToDds { get; set; } = true;

    /// <summary>
    /// 创建新国策后是否自动打开国策信息卡
    /// </summary>
    public bool IsAutoOpenFocusInfoCard { get; set; } = true;

    /// <summary>
    /// 文件树侧边栏宽度
    /// </summary>
    public double FileTreeWidth { get; set; } = 250;

    [JsonIgnore]
    public bool IsFirstRun { get; private init; }

    private const string SettingsFileName = "settings.json";

    protected override JsonTypeInfo<SettingsService> JsonTypeInfo =>
        SettingsServiceContext.Default.SettingsService;

    public static SettingsService LoadSettings()
    {
        return LoadInternal(
            Path.Combine(App.ConfigFolder, SettingsFileName),
            SettingsServiceContext.Default.SettingsService,
            afterLoadAction: settings =>
            {
                if (!settings.IsFirstRun)
                {
                    Log.Info(
                        "游戏根目录: {GamePath}, 存在: {GamePathExist}, MOD根目录: {ModPath}, 存在: {ModPathExist}",
                        settings.GameRootFolderPath,
                        Directory.Exists(settings.GameRootFolderPath),
                        settings.ModRootFolderPath,
                        Directory.Exists(settings.ModRootFolderPath)
                    );
                }
            },
            defaultFactory: () => new SettingsService { IsFirstRun = true }
        );
    }
}
