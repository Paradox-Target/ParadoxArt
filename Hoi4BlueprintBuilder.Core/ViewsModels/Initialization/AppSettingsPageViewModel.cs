using CommunityToolkit.Mvvm.ComponentModel;
using EnumsNET;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Initialization;

[RegisterTransient<AppSettingsPageViewModel>]
public sealed partial class AppSettingsPageViewModel(SettingsService settings) : ObservableObject
{
    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedGameLanguageIndex { get; set; }
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
