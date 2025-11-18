using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Services;
using Microsoft.Win32;
using NLog;

namespace Hoi4BlueprintEditor.ViewsModels;

[RegisterTransient<SettingsWindowViewModel>]
public sealed partial class SettingsWindowViewModel : ObservableObject
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

    [ObservableProperty]
    private int _selectedAppLanguagesIndex;

    [ObservableProperty]
    private int _selectedGameLanguagesIndex;

    private readonly SettingsService _settings;
    private bool _isChanged;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SettingsWindowViewModel(SettingsService settings)
    {
        _settings = settings;
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
    private void PickGameRootPath()
    {
        var dialog = new OpenFolderDialog { Multiselect = false };

        if (dialog.ShowDialog() == true)
        {
            GameRootFolderPath = dialog.FolderName;
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

    public void SaveIfChange()
    {
        if (_isChanged)
        {
            _settings.SaveSettings();
        }
    }
}
