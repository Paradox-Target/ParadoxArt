using CommunityToolkit.Mvvm.ComponentModel;
using EnumsNET;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Services.GameResources;
using Hoi4BlueprintEditor.Services.GameResources.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed partial class FocusInfoViewModel : ObservableObject
{
    public decimal Cost
    {
        get => FocusNode.Cost;
        set
        {
            FocusNode.Cost = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FocusDaysTip));
        }
    }

    public IReadOnlyList<GameLanguage> Languages { get; } = Enums.GetValues<GameLanguage>();

    [ObservableProperty]
    private string _idText;

    [ObservableProperty]
    private string _descriptionText;

    [ObservableProperty]
    private int _selectedLanguageIndex;

    public FocusInfoViewModel(FocusNode focusNode)
    {
        FocusNode = focusNode;

        _idText = LocalizationService.GetValue(FocusNode.Id);
        _descriptionText = LocalizationService.GetValue($"{FocusNode.Id}_desc");
        _selectedLanguageIndex = _lastSelectedLanguageIndex;
    }

    public FocusNode FocusNode { get; }

    public string IconPath =>
        SpriteService.TryGetSpriteFilePath(FocusNode.Icon, out string? path) ? path : string.Empty;

    public string FocusDaysTip => GetFocusDaysTip();

    private string GetFocusDaysTip()
    {
        int focusCost = DefinesService.Get<int>(DefineName);
        int totalDays = (int)(FocusNode.Cost * focusCost);
        return $" x {focusCost} = {totalDays} 天";
    }

    private static int _lastSelectedLanguageIndex = (int)
        App.Current.Services.GetRequiredService<SettingsService>().GameLanguage;
    private static readonly SpriteService SpriteService =
        App.Current.Services.GetRequiredService<SpriteService>();
    private static readonly DefinesService DefinesService =
        App.Current.Services.GetRequiredService<DefinesService>();
    private static readonly string[] DefineName = ["NDefines", "NFocus", "FOCUS_POINT_DAYS"];
    private static readonly LocalizationService LocalizationService =
        App.Current.Services.GetRequiredService<LocalizationService>();

    partial void OnIdTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        LocalizationService.AddOrUpdateLocalisation(
            FocusNode.Path,
            Languages[SelectedLanguageIndex],
            FocusNode.Id,
            value
        );
    }

    partial void OnDescriptionTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        LocalizationService.AddOrUpdateLocalisation(
            FocusNode.Path,
            Languages[SelectedLanguageIndex],
            $"{FocusNode.Id}_desc",
            value
        );
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        _lastSelectedLanguageIndex = value;
    }
}
