using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using LiveMarkdown.Avalonia;
using NLog;
using Velopack;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<AppUpdateViewModel>]
public sealed partial class AppUpdateViewModel : ObservableObject
{
    public bool ShowUpdateLog => HasUpdates && !IsDownloading;
    public string ProgressText => $"{Progress}%";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUpdateLog))]
    private bool _hasUpdates;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUpdateLog))]
    private bool _isDownloading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private double _progress;

    [ObservableProperty]
    private string _totalPackagesSize = string.Empty;

    [ObservableProperty]
    private string _newVersionText = string.Empty;

    public ObservableStringBuilder UpdateLog { get; } = new();

    private readonly UpdateManager _updateManager;
    private UpdateInfo? _updateInfo;
    private readonly MessageBoxService _messageBoxService;
    private readonly NavigationService _navigationService;
    private readonly TelemetryService _telemetryService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AppUpdateViewModel(
        MessageBoxService messageBoxService,
        NavigationService navigationService,
        TelemetryService telemetryService
    )
    {
        _messageBoxService = messageBoxService;
        _navigationService = navigationService;
        _telemetryService = telemetryService;
        if (Design.IsDesignMode)
        {
            _updateManager = null!;
            return;
        }

        _updateManager = new UpdateManager(App.UpdatePackageDownloadUrl);
        _ = CheckUpdateAsync();
    }

    private async Task CheckUpdateAsync()
    {
        try
        {
            long checkUpdateStartTimestamp = Stopwatch.GetTimestamp();
            _updateInfo = await _updateManager.CheckForUpdatesAsync();
            _telemetryService.TrackMetric(
                "check_update_elapsed_seconds",
                Stopwatch.GetElapsedTime(checkUpdateStartTimestamp).TotalSeconds
            );

            if (_updateInfo is null)
            {
                Log.Debug("已经是最新版本, 无需更新");
                NavigateIfDoNotUpdate();
                return;
            }

            HasUpdates = true;
            long totalBytes = 0;

            foreach (var asset in _updateInfo.DeltasToTarget)
            {
                UpdateLog.AppendLine(asset.NotesMarkdown);
                if (_updateInfo.DeltasToTarget.Length > 1)
                {
                    UpdateLog.AppendLine("---");
                }
                totalBytes += asset.Size;
            }
            TotalPackagesSize =
                $"升级包体积: {ByteSize
                .FromBytes(totalBytes)
                .MebiBytes.ToString("F2", CultureInfo.InvariantCulture)} MB";

            if (_updateInfo.DeltasToTarget.Length != 0)
            {
                NewVersionText = $"新版本: {_updateInfo.DeltasToTarget[^1].Version}, 当前版本: {App.Version}";
            }
            Log.Info("需要更新, 更新包数量: {Count}", _updateInfo.DeltasToTarget.Length);
        }
        catch (Exception e)
        {
            Log.Error(e, "检查更新失败");
            _telemetryService.TrackException(e, new Dictionary<string, string> { { "message", "检查更新失败" } });
            await _messageBoxService.ShowAsync("检查更新失败");
            NavigateIfDoNotUpdate();
        }
    }

    [RelayCommand]
    private async Task StartUpdateApp()
    {
        if (_updateInfo is null)
        {
            return;
        }

        IsDownloading = true;

        Log.Info("开始下载更新包");

        long totalBytes = _updateInfo.DeltasToTarget.AsValueEnumerable().Sum(asset => asset.Size);
        long startTimestamp = Stopwatch.GetTimestamp();
        await _updateManager.DownloadUpdatesAsync(_updateInfo, i => Progress = i);

        double elapsedSeconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;
        if (elapsedSeconds <= 0)
        {
            elapsedSeconds = 0.000001;
        }
        // 字节/秒
        double speedBps = totalBytes / elapsedSeconds;
        // 记录三个指标：速度(标准化)、耗时、大小
        var metrics = new Dictionary<string, double>
        {
            { "update_download_speed_bps", speedBps },
            { "update_download_duration_seconds", elapsedSeconds },
            { "update_package_size_bytes", totalBytes }
        };
        var properties = new Dictionary<string, string>
        {
            { "target_version", _updateInfo.TargetFullRelease.Version.ToString() }
        };
        _telemetryService.TrackEvent("update_packages_download_performance", properties, metrics);

        Log.Info("更新包下载完成");
        await App.Current.Services.DisposeAsync();
        _updateManager.ApplyUpdatesAndRestart(_updateInfo);
    }

    private void NavigateIfDoNotUpdate()
    {
        if (App.Current.IsActivated?.IsCompleted is true)
        {
            _navigationService.NavigateBasedOnDeviceStatus();
        }
        else
        {
            _navigationService.NavigateTo<LoadingView>();
        }
    }
}
