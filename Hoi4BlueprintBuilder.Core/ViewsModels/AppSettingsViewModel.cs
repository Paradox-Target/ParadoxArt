using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using LiveMarkdown.Avalonia;
using NLog;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<AppSettingsViewModel>]
public sealed partial class AppSettingsViewModel : ObservableObject
{
    public LanguageInfo[] AppLanguages { get; } = LanguageHelper.AppLanguages;
    public GameLanguage[] GameLanguages { get; } = Enum.GetValues<GameLanguage>();
    public ThemeMode[] ThemeModes { get; } = Enum.GetValues<ThemeMode>();
    public string[] InstalledFonts => InstalledFontsLazy.Value;

    private static readonly Lazy<string[]> InstalledFontsLazy =
        new(() => FontManager.Current.SystemFonts.AsValueEnumerable().Select(x => x.Name).Order().ToArray());

    public string SelectedFontFamily
    {
        get => _settings.MainFontFamily;
        set
        {
            if (_settings.MainFontFamily != value)
            {
                _settings.MainFontFamily = value;
                OnPropertyChanged();
                App.Current.UpdateApplicationFont(value);
            }
        }
    }

    public string SelectedCodeFontFamily
    {
        get => _settings.CodeFontFamily;
        set
        {
            if (_settings.CodeFontFamily != value)
            {
                _settings.CodeFontFamily = value;
                OnPropertyChanged();
                App.Current.UpdateApplicationCodeFont(value);
            }
        }
    }

    public bool IsAutoFocusPngConvertToDds
    {
        get => _settings.IsAutoFocusPngConvertToDds;
        set
        {
            _settings.IsAutoFocusPngConvertToDds = value;
            OnPropertyChanged();
        }
    }

    public bool IsAutoOpenFocusInfoCard
    {
        get => _settings.IsAutoOpenFocusInfoCard;
        set
        {
            _settings.IsAutoOpenFocusInfoCard = value;
            OnPropertyChanged();
        }
    }

    public bool EnableTelemetry
    {
        get => _settings.EnableTelemetry;
        set
        {
            _settings.EnableTelemetry = value;
            OnPropertyChanged();
        }
    }

    public string GameRootFolderPath
    {
        get => _settings.GameRootFolderPath;
        private set
        {
            _settings.GameRootFolderPath = value;
            OnPropertyChanged();
        }
    }

    public static string DotNetVersion => Environment.Version.ToString();

    [ObservableProperty]
    private int _selectedAppLanguagesIndex;

    [ObservableProperty]
    private int _selectedGameLanguagesIndex;

    [ObservableProperty]
    private int _selectedThemeModeIndex;

    [ObservableProperty]
    private string _selectedUpdateChannel;

    private readonly SettingsService _settings;
    private readonly FileService _fileService;
    private readonly TelemetryService _telemetryService;
    private readonly MessageBoxService _messageBoxService;
    private bool _isChanged;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AppSettingsViewModel(
        SettingsService settings,
        FileService fileService,
        TelemetryService telemetryService,
        MessageBoxService messageBoxService
    )
    {
        _settings = settings;
        _fileService = fileService;
        _telemetryService = telemetryService;
        _messageBoxService = messageBoxService;
        PropertyChanged += OnPropertyChangedEventHandler;

        _selectedAppLanguagesIndex = Array.FindIndex(
            AppLanguages,
            info => info.LanguageCode == settings.AppLanguage
        );
        if (_selectedAppLanguagesIndex == -1)
        {
            Log.Warn("未在 '{AppLanguages}' 中找到 App 语言: '{Language}'", AppLanguages, settings.AppLanguage);
            SelectedAppLanguagesIndex = 0;
        }

        _selectedGameLanguagesIndex = Array.FindIndex(
            GameLanguages,
            language => language == settings.GameLanguage
        );
        _selectedThemeModeIndex = Array.FindIndex(ThemeModes, mode => mode == settings.ThemeMode);
        _selectedUpdateChannel = _settings.AppUpdateChannel;
    }

    private void OnPropertyChangedEventHandler(object? o, PropertyChangedEventArgs eventArgs)
    {
        _isChanged = true;
        if (eventArgs.PropertyName is nameof(SelectedCodeFontFamily))
        {
            _telemetryService.TrackEvent("App_CodeFontFamily_Changed");
        }
        else if (eventArgs.PropertyName is nameof(SelectedFontFamily))
        {
            _telemetryService.TrackEvent("App_MainFontFamily_Changed");
        }
    }

    [RelayCommand]
    private async Task PickGameRootPath()
    {
        using var storageFolder = await _fileService.OpenFolderAsync();

        if (storageFolder is not null)
        {
            bool isExist = await storageFolder.GetFileAsync("hoi4.exe") is not null;
            if (!isExist)
            {
                await _messageBoxService
                    .ShowErrorAsync("未在选择目录中找到 hoi4.exe 文件, 请确认当前目录是游戏根目录")
                    .ConfigureAwait(false);
                return;
            }
            GameRootFolderPath = storageFolder.TryGetLocalPath() ?? throw new InvalidOperationException();
        }
    }

    [RelayCommand]
    private async Task OpenConfigFolderInExplorer(string configFolder)
    {
        Log.Debug("尝试打开配置文件夹: {ConfigFolder}", configFolder);
        try
        {
            await _fileService.LaunchUriAsync(configFolder).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await _messageBoxService.ShowErrorAsync("无法打开配置文件夹，可能文件夹不存在。", "打开配置文件夹失败").ConfigureAwait(false);
            Log.Error(e, "打开配置文件夹失败: {ConfigFolder}", configFolder);
        }
    }

    [RelayCommand]
    private async Task OpenLogsFolderInExplorer(string logsFolder)
    {
        Log.Debug("尝试打开日志文件夹: {LogsFolder}", logsFolder);
        try
        {
            await _fileService.LaunchUriAsync(logsFolder).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await _messageBoxService.ShowErrorAsync("无法打开日志文件夹，可能文件夹不存在。", "打开日志文件夹失败").ConfigureAwait(false);
            Log.Error(e, "打开日志文件夹失败: {LogsFolder}", logsFolder);
        }
    }

    [RelayCommand]
    private async Task OpenEulaAsync()
    {
        Log.Debug("尝试在应用内打开用户协议文本");
        try
        {
            string text = AssetLoadHelper.GetContentText("EULA.txt");

            var sb = new ObservableStringBuilder();
            sb.Append(text);

            var dialog = new FAContentDialog
            {
                Title = "用户协议 (EULA)",
                Content = new MarkdownRenderer { MarkdownBuilder = sb, Margin = new Thickness(10) },
                CloseButtonText = "关闭"
            };

            await dialog.ShowAsync();
        }
        catch (Exception e)
        {
            await _messageBoxService.ShowErrorAsync("无法打开用户协议文本。", "打开失败").ConfigureAwait(false);
            Log.Error(e, "打开用户协议失败");
        }
    }

    partial void OnSelectedAppLanguagesIndexChanged(int value)
    {
        string code = AppLanguages[value].LanguageCode;
        _settings.AppLanguage = code;
        Log.Info("App语言切换到: {Code}", code);
    }

    partial void OnSelectedGameLanguagesIndexChanged(int value)
    {
        var language = GameLanguages[value];
        _settings.GameLanguage = language;
        Log.Info("游戏语言切换到: {Code}", language);
    }

    partial void OnSelectedThemeModeIndexChanged(int value)
    {
        if (value < 0 || value >= ThemeModes.Length)
        {
            return;
        }

        var theme = ThemeModes[value];
        _settings.ThemeMode = theme;
        App.Current.RequestedThemeVariant = theme.ToThemeVariant();
    }

    partial void OnSelectedUpdateChannelChanged(string value)
    {
        _settings.AppUpdateChannel = value;
        Log.Info("应用更新频道切换到: {Channel}", value);
    }

    public void SaveIfChange()
    {
        if (_isChanged)
        {
            _settings.SaveSettings();
        }
    }
}
