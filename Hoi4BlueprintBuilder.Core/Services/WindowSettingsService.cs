// using System.Text;
// using System.Text.Json;
// using System.Text.Json.Serialization;
// using Avalonia.Controls;
// using NLog;
//
// namespace Hoi4BlueprintBuilder.Core.Services;
//
// public sealed class WindowSettingsService
// {
//     [JsonInclude]
//     [JsonPropertyName("windowInfo")]
//     private Dictionary<string, WindowSettings> _windowSettings = [];
//
//     private bool _isChanged;
//
//     private const string SettingsFileName = "windowSettings.json";
//     private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
//     private static readonly string SettingsFilePath = Path.Combine(App.ConfigFolder, SettingsFileName);
//     private static readonly Logger Log = LogManager.GetCurrentClassLogger();
//
//     public void SetWindow(Window window)
//     {
//         if (!_windowSettings.TryGetValue(GetKey(window), out var windowSettings))
//         {
//             return;
//         }
//
//         // 检查窗口位置是否在可视范围内（防止显示器配置变更导致窗口在屏幕外）
//         if (IsWindowVisible(windowSettings))
//         {
//             window.Left = windowSettings.Left;
//             window.Top = windowSettings.Top;
//         }
//         else
//         {
//             window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
//         }
//
//         window.Width = windowSettings.Width;
//         window.Height = windowSettings.Height;
//
//         if (windowSettings.IsMaximized)
//         {
//             window.WindowState = WindowState.Maximized;
//         }
//         Log.Info("窗口设置已应用到: {Window}", window.Title);
//     }
//
//     private static bool IsWindowVisible(WindowSettings settings)
//     {
//         var windowRect = new Rect(settings.Left, settings.Top, settings.Width, settings.Height);
//         var virtualScreen = new Rect(
//             SystemParameters.VirtualScreenLeft,
//             SystemParameters.VirtualScreenTop,
//             SystemParameters.VirtualScreenWidth,
//             SystemParameters.VirtualScreenHeight
//         );
//
//         return virtualScreen.IntersectsWith(windowRect);
//     }
//
//     public void SaveWindow(Window window)
//     {
//         string key = GetKey(window);
//
//         // 如果窗口是最大化或最小化状态，使用 RestoreBounds 获取还原后的位置和大小
//         // 否则直接使用当前属性
//         Rect bounds =
//             window.WindowState == WindowState.Normal
//                 ? new Rect(window.Left, window.Top, window.Width, window.Height)
//                 : window.RestoreBounds;
//
//         // 如果 RestoreBounds 无效（例如窗口从未显示过），回退到当前属性
//         if (bounds == Rect.Empty)
//         {
//             bounds = new Rect(window.Left, window.Top, window.Width, window.Height);
//         }
//
//         var windowSettings = new WindowSettings
//         {
//             Width = bounds.Width,
//             Height = bounds.Height,
//             Top = bounds.Top,
//             Left = bounds.Left,
//             IsMaximized = window.WindowState == WindowState.Maximized
//         };
//         if (_windowSettings.TryGetValue(key, out var value) && value == windowSettings)
//         {
//             return;
//         }
//
//         _windowSettings[key] = windowSettings;
//         _isChanged = true;
//     }
//
//     private static string GetKey(Window window)
//     {
//         return $"{window.GetType().FullName}";
//     }
//
//     private sealed record WindowSettings
//     {
//         public double Width { get; set; }
//         public double Height { get; set; }
//         public double Top { get; set; }
//         public double Left { get; set; }
//         public bool IsMaximized { get; set; }
//     }
//
//     public static WindowSettingsService LoadSettings()
//     {
//         Log.Info("尝试加载配置文件: {SettingsFilePath}", SettingsFilePath);
//         if (!File.Exists(SettingsFilePath))
//         {
//             return new WindowSettingsService();
//         }
//
//         try
//         {
//             string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
//             return JsonSerializer.Deserialize<WindowSettingsService>(json) ?? new WindowSettingsService();
//         }
//         catch (Exception ex)
//         {
//             Log.Error(ex, "加载配置文件失败，使用默认设置");
//             return new WindowSettingsService();
//         }
//     }
//
//     public void SaveSettings()
//     {
//         if (!_isChanged)
//         {
//             Log.Info("窗口设置未更改，跳过保存");
//             return;
//         }
//         try
//         {
//             string json = JsonSerializer.Serialize(this, Options);
//             File.WriteAllText(SettingsFilePath, json, Encoding.UTF8);
//             _isChanged = false;
//             Log.Info("已成功保存配置文件: {SettingsFilePath}", SettingsFilePath);
//         }
//         catch (Exception ex)
//         {
//             Log.Error(ex, "保存配置文件失败");
//         }
//     }
// }
