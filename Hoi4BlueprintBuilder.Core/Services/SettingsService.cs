using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

public sealed class SettingsService
{
    public string AppLanguage { get; set; } = string.Empty;
    public GameLanguage GameLanguage { get; set; } =
        LanguageHelper.GetGameLanguageBySystemLanguage();
    public string GameRootFolderPath { get; set; } = string.Empty;
    public string ModRootFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// 导入国策图片时是否自动将 png 转换为 dds 格式
    /// </summary>
    public bool IsAutoFocusPngConvertToDds { get; set; } = true;

    [JsonIgnore]
    public bool IsFirstRun { get; private init; }

    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static readonly string SettingsFilePath = Path.Combine(
        App.ConfigFolder,
        SettingsFileName
    );
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static SettingsService LoadSettings()
    {
        Log.Info("尝试加载配置文件: {SettingsFilePath}", SettingsFilePath);
        if (!File.Exists(SettingsFilePath))
        {
            return new SettingsService { IsFirstRun = true };
        }

        try
        {
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<SettingsService>(json)
                ?? new SettingsService { IsFirstRun = true };
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
            string json = JsonSerializer.Serialize(this, Options);
            File.WriteAllText(SettingsFilePath, json, Encoding.UTF8);
            Log.Info("已成功保存配置文件: {SettingsFilePath}", SettingsFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存配置文件失败");
        }
    }
}
