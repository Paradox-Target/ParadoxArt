using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models;

namespace Hoi4BlueprintEditor.ViewModels;

public partial class FocusNodeViewModel : ObservableObject
{
    public FocusNodeModel Model { get; }

    public string Id => Model.Id;
    public string Icon => Model.Icon;

    [ObservableProperty]
    private int _x;

    [ObservableProperty]
    private int _y;

    public FocusNodeViewModel(FocusNodeModel model)
    {
        Model = model;
        _x = model.X;
        _y = model.Y;
    }
}
