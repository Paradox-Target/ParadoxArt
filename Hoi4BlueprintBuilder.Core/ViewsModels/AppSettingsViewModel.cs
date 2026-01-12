using System.ComponentModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
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
        set
        {
            _settings.IsAutoFocusPngConvertToDds = value;
            OnPropertyChanged();
        }
        get => _settings.IsAutoFocusPngConvertToDds;
    }

    public bool IsAutoOpenFocusInfoCard
    {
        set
        {
            _settings.IsAutoOpenFocusInfoCard = value;
            OnPropertyChanged();
        }
        get => _settings.IsAutoOpenFocusInfoCard;
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

    public string ModRootFolderPath
    {
        get => _settings.ModRootFolderPath;
        private set
        {
            _settings.ModRootFolderPath = value;
            OnPropertyChanged();
        }
    }

    public string DotNetVersion => Environment.Version.ToString();

    [ObservableProperty]
    private int _selectedAppLanguagesIndex;

    [ObservableProperty]
    private int _selectedGameLanguagesIndex;

    [ObservableProperty]
    private int _selectedThemeModeIndex;

    private readonly SettingsService _settings;
    private readonly FileService _fileService;
    private readonly TelemetryService _telemetryService;
    private bool _isChanged;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AppSettingsViewModel(
        SettingsService settings,
        FileService fileService,
        TelemetryService telemetryService
    )
    {
        _settings = settings;
        _fileService = fileService;
        _telemetryService = telemetryService;
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
        var dialog = await _fileService.OpenFolderAsync();

        if (dialog is not null)
        {
            GameRootFolderPath = dialog.Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task PickModRootFolderPath()
    {
        var dialog = await _fileService.OpenFolderAsync();
        if (dialog is not null)
        {
            ModRootFolderPath = dialog.Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task OpenConfigFolderInExplorer(string configFolder)
    {
        Log.Debug("尝试打开配置文件夹: {ConfigFolder}", configFolder);
        await _fileService.LaunchUriAsync(configFolder);
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

    public void SaveIfChange()
    {
        if (_isChanged)
        {
            _settings.SaveSettings();
        }
    }
}
