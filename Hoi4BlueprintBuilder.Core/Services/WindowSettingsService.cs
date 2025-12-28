using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

public sealed class WindowSettingsService
{
    [JsonInclude]
    [JsonPropertyName("windowInfo")]
    private Dictionary<string, WindowSettings> _windowSettings = [];

    private bool _isChanged;

    private const string SettingsFileName = "windowSettings.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static readonly string SettingsFilePath = Path.Combine(App.ConfigFolder, SettingsFileName);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void SetWindow(Window window)
    {
        if (!_windowSettings.TryGetValue(GetKey(window), out var windowSettings))
        {
            return;
        }

        window.Width = windowSettings.Width;
        window.Height = windowSettings.Height;

        if (windowSettings.IsMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
        Log.Info("窗口设置已应用到: {Window}", window.Title);
    }

    public void SaveWindow(Window window)
    {
        string key = GetKey(window);

        var windowSettings = new WindowSettings
        {
            Width = window.Width,
            Height = window.Height,
            IsMaximized = window.WindowState == WindowState.Maximized
        };
        if (_windowSettings.TryGetValue(key, out var value) && value == windowSettings)
        {
            return;
        }

        _windowSettings[key] = windowSettings;
        _isChanged = true;
    }

    private static string GetKey(Window window)
    {
        return $"{window.GetType().FullName}";
    }

    private sealed record WindowSettings
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMaximized { get; set; }
    }

    public static WindowSettingsService LoadSettings()
    {
        Log.Info("尝试加载配置文件: {SettingsFilePath}", SettingsFilePath);
        if (!File.Exists(SettingsFilePath))
        {
            Log.Info("未找到配置文件，使用默认设置");
            return new WindowSettingsService();
        }

        try
        {
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            var result = JsonSerializer.Deserialize<WindowSettingsService>(json);
            if (result is null)
            {
                result = new WindowSettingsService();
                Log.Warn("配置文件解析失败，使用默认设置");
            }
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载配置文件失败，使用默认设置");
            return new WindowSettingsService();
        }
    }

    public void SaveSettings()
    {
        if (!_isChanged)
        {
            Log.Info("窗口设置未更改，跳过保存");
            return;
        }
        try
        {
            string json = JsonSerializer.Serialize(this, Options);
            File.WriteAllText(SettingsFilePath, json, Encoding.UTF8);
            _isChanged = false;
            Log.Info("已成功保存窗口配置文件: {SettingsFilePath}", SettingsFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存窗口配置文件失败");
        }
    }
}
