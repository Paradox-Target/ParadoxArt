using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.ViewModels;

public sealed partial class FocusNodeViewModel : ObservableObject
{
    public FocusNode Model { get; }

    public string Id => Model.Id;
    public string Icon => Model.Icon;

    public int X => Model.RawPosition.X;
    public int Y => Model.RawPosition.Y;

    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
    }
}
