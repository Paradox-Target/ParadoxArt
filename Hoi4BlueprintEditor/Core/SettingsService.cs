using System;
using System.IO;
using System.Text.Json;
using Hoi4BlueprintEditor.Models;

namespace Hoi4BlueprintEditor.Core;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    public SettingsModel CurrentSettings { get; private set; } = new();

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolderPath = Path.Combine(appDataPath, "Hoi4BlueprintEditor");

        Directory.CreateDirectory(appFolderPath);
        _settingsFilePath = Path.Combine(appFolderPath, "settings.json");
    }

    public void LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            // 默认设置
            CurrentSettings = new SettingsModel();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            CurrentSettings =
                JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
        }
        catch (Exception)
        {
            // 默认设置
            CurrentSettings = new SettingsModel();
        }
    }

    public void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(
                CurrentSettings,
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception)
        {
            //
        }
    }
}
