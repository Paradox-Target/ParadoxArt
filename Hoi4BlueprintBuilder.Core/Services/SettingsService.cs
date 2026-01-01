using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SettingsService))]
internal partial class SettingsServiceContext : JsonSerializerContext;

public sealed class SettingsService
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

    [JsonIgnore]
    public bool IsFirstRun { get; private init; }

    private const string SettingsFileName = "settings.json";
    private static readonly string SettingsFilePath = Path.Combine(App.ConfigFolder, SettingsFileName);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static SettingsService LoadSettings()
    {
        Log.Info("尝试加载配置文件: {SettingsFilePath}", SettingsFilePath);
        if (!File.Exists(SettingsFilePath))
        {
            Log.Info("配置文件不存在, 使用默认设置.");
            return new SettingsService { IsFirstRun = true };
        }

        try
        {
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<SettingsService>(
                json,
                SettingsServiceContext.Default.SettingsService
            );
            if (settings is null)
            {
                settings = new SettingsService { IsFirstRun = true };
                Log.Warn("反序列化设置失败, 设置为默认值.");
            }
            else
            {
                Log.Info(
                    "游戏根目录: {GamePath}, 存在: {GamePathExist}, MOD根目录: {ModPath}, 存在: {ModPathExist}",
                    settings.GameRootFolderPath,
                    Directory.Exists(settings.GameRootFolderPath),
                    settings.ModRootFolderPath,
                    Directory.Exists(settings.ModRootFolderPath)
                );
            }
            return settings;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载配置文件失败，使用默认设置");
            return new SettingsService { IsFirstRun = true };
        }
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, SettingsServiceContext.Default.SettingsService);
            File.WriteAllText(SettingsFilePath, json, Encoding.UTF8);
            Log.Info("已成功保存配置文件: {SettingsFilePath}", SettingsFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存配置文件失败");
        }
    }
}
