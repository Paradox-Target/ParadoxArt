using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.ViewModels;

public sealed partial class FocusNodeViewModel : ObservableObject
{
    public FocusNode Model { get; }

    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
    }
}
