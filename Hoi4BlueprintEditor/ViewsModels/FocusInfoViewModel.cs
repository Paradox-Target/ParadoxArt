using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services.GameResources;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed class FocusInfoViewModel(FocusNode focusNode) : ObservableObject
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
    public FocusNode FocusNode { get; } = focusNode;

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
}
