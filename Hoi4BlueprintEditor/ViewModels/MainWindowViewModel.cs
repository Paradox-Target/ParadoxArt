using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Core;
using Hoi4BlueprintEditor.Messages;
using Microsoft.Win32;

namespace Hoi4BlueprintEditor.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _localizationService;

    public EditorCanvasViewModel EditorCanvas { get; }

    public MainWindowViewModel(
        SettingsService settingsService,
        LocalizationService localizationService,
        EditorCanvasViewModel editorCanvasViewModel
    )
    {
        _settingsService = settingsService;
        _localizationService = localizationService;

        EditorCanvas = editorCanvasViewModel;
    }

    private void SetLanguage(string cultureCode)
    {
        var targetCulture = new CultureInfo(cultureCode);

        string message = _localizationService.GetString("LanguageRestartMessage", targetCulture);
        string title = _localizationService.GetString("RestartRequiredTitle", targetCulture);

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
}
