using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Services;

namespace Hoi4BlueprintEditor.ViewsModels.Initialization;

[RegisterTransient<AppSettingsPageViewModel>]
public sealed partial class AppSettingsPageViewModel(SettingsService settings) : ObservableObject
{
    [ObservableProperty]
    private int _selectedIndex = 0;
    public string[] Languages { get; } = ["简体中文 | zh-CN", "English | en-US"];

    partial void OnSelectedIndexChanged(int value)
    {
        settings.Language = Languages[value].Split('|', StringSplitOptions.TrimEntries)[1];
    }
}