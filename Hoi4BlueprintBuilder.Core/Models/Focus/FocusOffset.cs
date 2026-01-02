using CommunityToolkit.Mvvm.ComponentModel;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed partial class FocusOffset(FocusPoint offset, Node? trigger) : ObservableObject, IFocusTrigger
{
    public FocusPoint Offset { get; } = offset;
    public Node? Trigger { get; } = trigger;

    [ObservableProperty]
    private bool _isEnabled;
}
