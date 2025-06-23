using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services.GameResources.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewModels;

public sealed partial class FocusNodeViewModel : ObservableObject
{
    public FocusNode Model { get; }
    public string LocalizedName => LocalizationService.GetFormatText(Model.Id);

    private static readonly LocalizationFormatService LocalizationService =
        App.Current.Services.GetRequiredService<LocalizationFormatService>();

    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
    }
}
