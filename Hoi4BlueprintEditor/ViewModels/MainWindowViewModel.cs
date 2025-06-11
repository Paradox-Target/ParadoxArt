using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Core;

namespace Hoi4BlueprintEditor.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;

    public MainWindowViewModel(
        ISettingsService settingsService,
        ILocalizationService localizationService
    )
    {
        _settingsService = settingsService;
        _localizationService = localizationService;
    }

    private void SetLanguage(string cultureCode)
    {
        var targetCulture = new CultureInfo(cultureCode);

        var message = _localizationService.GetString("LanguageRestartMessage", targetCulture);
        var title = _localizationService.GetString("RestartRequiredTitle", targetCulture);

        _settingsService.CurrentSettings.Language = cultureCode;
        _settingsService.SaveSettings();

        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SetLanguageToEnglish() => SetLanguage("en-US");

    [RelayCommand]
    private void SetLanguageToChinese() => SetLanguage("zh-CN");
}
