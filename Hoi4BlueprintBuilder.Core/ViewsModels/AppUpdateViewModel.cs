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
            _updateInfo = await _updateManager.CheckForUpdatesAsync();
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
        Log.Info("准备开始更新软件");
        await _updateManager.DownloadUpdatesAsync(_updateInfo, i => Progress = i);

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
