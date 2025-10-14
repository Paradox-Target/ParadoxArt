using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Services;
using Microsoft.Win32;

namespace Hoi4BlueprintEditor.ViewModels;

[RegisterSingleton<MainControlViewModel>]
public sealed partial class MainControlViewModel : ObservableObject 
{
    private readonly SettingsService _settingsService;
    private readonly AppLocalizationService _appLocalizationService;

    public MainControlViewModel(SettingsService settingsService, AppLocalizationService appLocalizationService)
    {
        _settingsService = settingsService;
        _appLocalizationService = appLocalizationService;
    }

    private void SetLanguage(string cultureCode)
    {
        var targetCulture = new CultureInfo(cultureCode);

        string message = _appLocalizationService.GetString("LanguageRestartMessage", targetCulture);
        string title = _appLocalizationService.GetString("RestartRequiredTitle", targetCulture);

        _settingsService.Language = cultureCode;
        _settingsService.SaveSettings();

        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SetLanguageToEnglish() => SetLanguage("en-US");

    [RelayCommand]
    private void SetLanguageToChinese() => SetLanguage("zh-CN");

    [RelayCommand]
    private void OpenFocusFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Focus Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "选择国策树文件",
            Multiselect = false,
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        string filePath = openFileDialog.FileName;
        WeakReferenceMessenger.Default.Send(new OpenFileMessage(filePath));
    }

    [RelayCommand]
    private void SaveFocusFile()
    {
        WeakReferenceMessenger.Default.Send(new SaveFocusTreeMessage(""));
    }
}