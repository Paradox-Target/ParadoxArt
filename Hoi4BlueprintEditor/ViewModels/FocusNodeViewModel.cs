using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.ViewModels;

public partial class FocusNodeViewModel : ObservableObject
{
    public FocusNode Model { get; }

    public string Id => Model.Id;
    public string Icon => Model.Icon;

    public int X => Model.Position.X;
    public int Y => Model.Position.Y;

    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
    }
}
