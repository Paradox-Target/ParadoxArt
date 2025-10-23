using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed partial class FocusInfoViewModel(FocusNode focusNode) : ObservableObject
{
    private readonly FocusNode _focusNode = focusNode;

    public string Id
    {
        get => _focusNode.Id;
        set
        {
            _focusNode.Id = value;
            OnPropertyChanged();
        }
    }

    public decimal Cost
    {
        get => _focusNode.Cost;
        set
        {
            _focusNode.Cost = value;
            OnPropertyChanged();
        }
    }
}