using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using NLog;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<AppSettingsViewModel>]
public sealed partial class AppSettingsViewModel : ObservableObject
{
    public LanguageInfo[] AppLanguages { get; } = LanguageHelper.AppLanguages;
    public GameLanguage[] GameLanguages { get; } = Enum.GetValues<GameLanguage>();

    public bool IsAutoFocusPngConvertToDds
    {
        set
        {
            _settings.IsAutoFocusPngConvertToDds = value;
            OnPropertyChanged();
        }
        get => _settings.IsAutoFocusPngConvertToDds;
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

    public string DotNetVersion => Environment.Version.ToString();

    [ObservableProperty]
    private int _selectedAppLanguagesIndex;

    [ObservableProperty]
    private int _selectedGameLanguagesIndex;

    private readonly SettingsService _settings;
    private readonly FileService _fileService;
    private bool _isChanged;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AppSettingsViewModel(SettingsService settings, FileService fileService)
    {
        _settings = settings;
        _fileService = fileService;
        PropertyChanged += (_, _) => _isChanged = true;

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
    }

    [RelayCommand]
    private async Task PickGameRootPath()
    {
        var dialog = await _fileService.OpenFileAsync();

        if (dialog is not null)
        {
            GameRootFolderPath = dialog.Path.LocalPath;
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

    public void SaveIfChange()
    {
        if (_isChanged)
        {
            _settings.SaveSettings();
        }
    }
}
