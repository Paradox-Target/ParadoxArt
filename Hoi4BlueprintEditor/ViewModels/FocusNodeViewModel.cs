using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.ViewModels;

public partial class FocusNodeViewModel : ObservableObject
{
    public FocusNode Model { get; }
    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
    }
}
