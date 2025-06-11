using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Hoi4BlueprintEditor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private void SetLanguage(string cultureCode)
    {
        var newCulture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentUICulture = newCulture;
        CultureInfo.CurrentUICulture = newCulture;
        CultureInfo.CurrentCulture = newCulture;
    }

    [RelayCommand]
    private void SetLanguageToEnglish() => SetLanguage("en-US");

    [RelayCommand]
    private void SetLanguageToChinese() => SetLanguage("zh-CN");
}