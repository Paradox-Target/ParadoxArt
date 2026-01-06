using System.Diagnostics;
using System.Globalization;
using ByteSizeLib;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<TelemetryService>]
public sealed class TelemetryService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly TelemetryClient _client;
    private readonly Timer _memoryMonitorTimer;
    private readonly Process _currrntProcess = Process.GetCurrentProcess();

    public TelemetryService(DeviceService deviceService, SettingsService settingsService)
    {
        _settingsService = settingsService;
        var config = TelemetryConfiguration.CreateDefault();
#if DEBUG
        config.DisableTelemetry = true;
#endif
        config.ConnectionString = Private.ApplicationInsightsConnectionString;
        _client = new TelemetryClient(config);
        _client.Context.User.Id = deviceService.GetDeviceId();

        _client.Context.Session.Id = Guid.NewGuid().ToString();
        _client.Context.Session.IsFirst = settingsService.IsFirstRun;

        _memoryMonitorTimer = new Timer(
            _ => MemoryUsage(),
            null,
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(20)
        );
    }

    private void MemoryUsage()
    {
        try
        {
            _currrntProcess.Refresh();
            long currentMemory = _currrntProcess.PrivateMemorySize64;
            double memoryInMB = ByteSize.FromBytes(currentMemory).MegaBytes;

            GetMetric("App_Memory_Usage_MB").TrackValue(memoryInMB);
        }
        catch (Exception)
        {
            // 忽略采样错误
        }
    }

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        _client.TrackEvent(eventName, properties);
    }

    public void TrackMetric(string name, double value)
    {
        _client.TrackMetric(name, value);
    }

    private Metric GetMetric(string name)
    {
        return _client.GetMetric(name);
    }

    public void TrackSystemEnvironment(string? screenSize = null, string? screenScaling = null)
    {
        try
        {
            var deviceMemory = ByteSize.FromBytes(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes);
            var properties = new Dictionary<string, string>
            {
                { "App_Version", App.Version.ToString() },
                { "OS_Version", Environment.OSVersion.ToString() },
                { "Culture", CultureInfo.CurrentUICulture.Name },
                { "Processor_Count", Environment.ProcessorCount.ToString() },
                {
                    "Memory_Size",
                    $"{deviceMemory.GigaBytes.ToString("F1", CultureInfo.InvariantCulture)} GB"
                },
                { "Screen_Size", screenSize ?? "Unknown" },
                { "Screen_Scaling", screenScaling ?? "Unknown" }
            };
            // 第一次运行时用户还没有选择主题模式，所以这里不记录
            if (!_settingsService.IsFirstRun)
            {
                properties["App_Theme_Mode"] = _settingsService.ThemeMode.ToString();
                properties["App_Language"] = _settingsService.AppLanguage;
                properties["Game_Language"] = _settingsService.GameLanguage.ToString();
            }

            TrackEvent("App_Environment_Snapshop", properties);
        }
        catch (Exception)
        {
            // 忽略
        }
    }

    public void TrackAppMemoryUsage()
    {
        try
        {
            _currrntProcess.Refresh();
            // 记录退出时的瞬时内存，作为单独的数据点
            var finalMemory = ByteSize.FromBytes(_currrntProcess.PrivateMemorySize64);
            TrackMetric("App_Memory_Final_MB", finalMemory.MegaBytes);
        }
        catch (Exception)
        {
            // 忽略
        }
    }

    public void Dispose()
    {
        _memoryMonitorTimer.Dispose();
        _currrntProcess.Dispose();
        _client.Flush();
    }
}
