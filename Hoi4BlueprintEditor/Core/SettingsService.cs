using System.IO;
using System.Text;
using System.Text.Json;

namespace Hoi4BlueprintEditor.Core;

public sealed class SettingsService
{
    public string Language { get; set; } = string.Empty;

    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static readonly string SettingsFilePath = Path.Combine(
        App.ConfigFolder,
        SettingsFileName
    );

    public static SettingsService LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
        {
            // 默认设置
            return new SettingsService();
        }

        try
        {
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<SettingsService>(json) ?? new SettingsService();
        }
        catch (Exception)
        {
            // 默认设置
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
        catch (Exception)
        {
            // ignored
        }
    }
}
