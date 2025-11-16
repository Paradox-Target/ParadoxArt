using CommunityToolkit.Mvvm.ComponentModel;
using EnumsNET;
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

    public LanguageInfo[] Languages { get; } = [new("简体中文", "zh-CN"), new("English", "en-US")];
    public IReadOnlyList<GameLanguage> GameLanguages { get; } = Enums.GetValues<GameLanguage>();

    partial void OnSelectedIndexChanged(int value)
    {
        settings.Language = Languages[value].LanguageCode;
    }

    partial void OnSelectedGameLanguageIndexChanged(int value)
    {
        settings.GameLanguage = GameLanguages[value];
    }

    public sealed record LanguageInfo(string DisplayName, string LanguageCode);
}
