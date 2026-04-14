using System.Diagnostics;
using System.Globalization;
using ByteSizeLib;
using Hardware.Info;
using Hoi4BlueprintBuilder.Core.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<TelemetryService>]
public sealed class TelemetryService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly TelemetryClient _client;

    public TelemetryService(
        DeviceService deviceService,
        SettingsService settingsService,
        StatusBarService statusBarService
    )
    {
        _settingsService = settingsService;
        var config = TelemetryConfiguration.CreateDefault();

        config.DisableTelemetry = !_settingsService.EnableTelemetry;

        config.ConnectionString = Private.ApplicationInsightsConnectionString;
        _client = new TelemetryClient(config);
        _client.Context.User.Id = deviceService.GetDeviceId();

        _client.Context.Session.Id = Guid.NewGuid().ToString();
        _client.Context.Session.IsFirst = settingsService.IsFirstRun;
        statusBarService.UpdateRamBytesUsage += ram =>
        {
            double mb = ByteSize.FromBytes(ram).MebiBytes;
            var metric = GetMetric("App_Memory_Usage_MB");
            metric.TrackValue(mb);
        };
    }

    public void TrackEvent(
        string eventName,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null
    )
    {
        _client.TrackEvent(eventName, properties, metrics);
    }

    public void TrackMetric(string name, double value)
    {
        _client.TrackMetric(name, value);
    }

    public void TrackException(Exception exception, string message)
    {
        _client.TrackException(exception, new Dictionary<string, string> { { "message", message } });
    }

    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        _client.TrackException(exception, properties);
    }

    private Metric GetMetric(string name)
    {
        return _client.GetMetric(name);
    }

    public void TrackSystemEnvironment(
        CultureInfo cultureInfo,
        string? screenSize = null,
        string? screenScaling = null
    )
    {
        try
        {
            var properties = new Dictionary<string, string>
            {
                { "App_Version", App.Version.ToString() },
                { "App_Update_Channel", _settingsService.AppUpdateChannel },
                { "OS_Version", Environment.OSVersion.ToString() },
                { "Culture", cultureInfo.Name },
                { "Platform", GetPlatformName() }
            };
            // 第一次运行时用户还没有选择主题模式，所以这里不记录
            if (!_settingsService.IsFirstRun)
            {
                properties["App_Theme_Mode"] = _settingsService.ThemeMode.ToString();
                properties["App_Language"] = _settingsService.AppLanguage;
                properties["Game_Language"] = _settingsService.GameLanguage.ToString();
            }

            Task.Run(() => TrackHardwareInfo(screenSize, screenScaling));

            TrackEvent("App_Environment_Snapshop", properties);
        }
        catch (Exception)
        {
            // 忽略
        }
    }

    private static string GetPlatformName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "Windows";
        }

        if (OperatingSystem.IsLinux())
        {
            return "Linux";
        }

        return "Unknown";
    }

    private void TrackHardwareInfo(string? screenSize, string? screenScaling)
    {
        // 我们只在第一次运行时记录硬件信息
        if (OperatingSystem.IsMobile || !_settingsService.IsFirstRun)
        {
            return;
        }

        try
        {
            var hardwareInfo = new HardwareInfo();
            hardwareInfo.RefreshCPUList();
            hardwareInfo.RefreshVideoControllerList();
            hardwareInfo.RefreshMemoryList();
            hardwareInfo.RefreshOperatingSystem();

            double totalMemory = ByteSize
                .FromBytes(hardwareInfo.MemoryList.AsValueEnumerable().Sum(memory => memory.Capacity))
                .GibiBytes;

            var properties = new Dictionary<string, string>
            {
                {
                    "CPU_Names",
                    hardwareInfo.CpuList.AsValueEnumerable().Select(cpu => cpu.Name).JoinToString(',')
                },
                {
                    "GPU_Names",
                    hardwareInfo
                        .VideoControllerList.AsValueEnumerable()
                        .Select(gpu => gpu.Name)
                        .JoinToString(',')
                },
                { "Memory_Total_GB", $"{totalMemory.ToString("F1", CultureInfo.InvariantCulture)} GB" },
                { "OS_Name", hardwareInfo.OperatingSystem.Name },
                { "Screen_Size", screenSize ?? "Unknown" },
                { "Screen_Scaling", screenScaling ?? "Unknown" },
                { "Processor_Count", Environment.ProcessorCount.ToString() }
            };
            TrackEvent("Hardware_Snapshop", properties);
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
            using var currentProcess = Process.GetCurrentProcess();
            // 记录退出时的瞬时内存，作为单独的数据点
            var finalMemory = ByteSize.FromBytes(currentProcess.PrivateMemorySize64);
            TrackMetric("App_Memory_Final_MB", finalMemory.MegaBytes);
        }
        catch (Exception)
        {
            // 忽略
        }
    }

    public void Dispose()
    {
        _client.Flush();
    }
}
