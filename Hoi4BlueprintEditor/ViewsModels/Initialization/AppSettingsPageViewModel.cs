using CommunityToolkit.Mvvm.ComponentModel;
using EnumsNET;
using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Services;

namespace Hoi4BlueprintEditor.ViewsModels.Initialization;

[RegisterTransient<AppSettingsPageViewModel>]
public sealed partial class AppSettingsPageViewModel(SettingsService settings) : ObservableObject
{
    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private int _selectedGameLanguageIndex;

    public LanguageInfo[] Languages { get; } = LanguageHelper.AppLanguages;
    public IReadOnlyList<GameLanguage> GameLanguages { get; } = Enums.GetValues<GameLanguage>();

    partial void OnSelectedIndexChanged(int value)
    {
        settings.AppLanguage = Languages[value].LanguageCode;
    }

    partial void OnSelectedGameLanguageIndexChanged(int value)
    {
        settings.GameLanguage = GameLanguages[value];
    }
}
