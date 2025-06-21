using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services.GameResources.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewModels;

public sealed partial class FocusNodeViewModel : ObservableObject
{
    public FocusNode Model { get; }
    public string LocalizedName => LocalizationService.GetValue(Model.Id);

    private static readonly LocalizationService LocalizationService =
        App.Current.Services.GetRequiredService<LocalizationService>();

    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
    }
}
