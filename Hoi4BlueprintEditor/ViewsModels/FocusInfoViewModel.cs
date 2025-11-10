using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
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

    [ObservableProperty]
    private string _idText;

    [ObservableProperty]
    private string _descriptionText;

    public FocusInfoViewModel(FocusNode focusNode)
    {
        FocusNode = focusNode;

        _idText = LocalizationService.GetValue(FocusNode.Id);
        _descriptionText = LocalizationService.GetValue($"{FocusNode.Id}_desc");
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

    private static readonly SpriteService SpriteService =
        App.Current.Services.GetRequiredService<SpriteService>();
    private static readonly DefinesService DefinesService =
        App.Current.Services.GetRequiredService<DefinesService>();
    private static readonly string[] DefineName = "NDefines.NFocus.FOCUS_POINT_DAYS".Split('.');
    private static readonly LocalizationService LocalizationService =
        App.Current.Services.GetRequiredService<LocalizationService>();

    partial void OnIdTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        LocalizationService.AddLocalisation(FocusNode.Path, FocusNode.Id, value);
    }

    partial void OnDescriptionTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        LocalizationService.AddLocalisation(FocusNode.Path, $"{FocusNode.Id}_desc", value);
    }
}
