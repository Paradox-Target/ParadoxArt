using System.IO;
using System.Text;
using System.Text.Json;
using NLog;

namespace Hoi4BlueprintEditor.Services;

public sealed class SettingsService
{
    public string Language { get; set; } = string.Empty;
    public string GameRootFolderPath { get; set; } = string.Empty;
    public string ModRootFolderPath { get; set; } = string.Empty;

    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static readonly string SettingsFilePath = Path.Combine(App.ConfigFolder, SettingsFileName);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static SettingsService LoadSettings()
    {
        Log.Info("尝试加载配置文件: {SettingsFilePath}", SettingsFilePath);
        if (!File.Exists(SettingsFilePath))
        {
            return new SettingsService();
        }

        try
        {
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<SettingsService>(json) ?? new SettingsService();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载配置文件失败，使用默认设置");
            return new SettingsService();
        }
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, Options);
            File.WriteAllText(SettingsFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存配置文件失败");
        }
        Log.Info("已成功保存配置文件: {SettingsFilePath}", SettingsFilePath);
    }
}
