using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Avalonia.Controls;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(WindowSettingsService))]
internal partial class WindowSettingsServiceContext : JsonSerializerContext;

public sealed class WindowSettingsService : BaseSettingsService<WindowSettingsService>
{
    [JsonInclude]
    [JsonPropertyName("windowInfo")]
    internal Dictionary<string, WindowSettings> _windowSettings = [];

    private bool _isChanged;

    private static new readonly Logger Log = LogManager.GetCurrentClassLogger();

    protected override string FileName => SettingsFileName;
    protected override JsonTypeInfo<WindowSettingsService> JsonTypeInfo =>
        WindowSettingsServiceContext.Default.WindowSettingsService;
    private const string SettingsFileName = "windowSettings.json";

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

    internal sealed record WindowSettings
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMaximized { get; set; }
    }

    public static WindowSettingsService LoadSettings()
    {
        return LoadInternal(
            Path.Combine(App.ConfigFolder, SettingsFileName),
            WindowSettingsServiceContext.Default.WindowSettingsService
        );
    }

    public override void SaveSettings()
    {
        if (!_isChanged)
        {
            Log.Info("窗口设置未更改，跳过保存");
            return;
        }

        base.SaveSettings();
        _isChanged = false;
    }
}
