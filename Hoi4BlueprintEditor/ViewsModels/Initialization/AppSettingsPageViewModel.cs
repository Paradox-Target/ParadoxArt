using CommunityToolkit.Mvvm.ComponentModel;
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

    public string[] Languages { get; } = ["简体中文 | zh-CN", "English | en-US"];
    public GameLanguage[] GameLanguages { get; } = Enum.GetValues<GameLanguage>();

    partial void OnSelectedIndexChanged(int value)
    {
        settings.Language = Languages[value].Split('|', StringSplitOptions.TrimEntries)[1];
    }

    partial void OnSelectedGameLanguageIndexChanged(int value)
    {
        settings.GameLanguage = GameLanguages[value];
    }
}