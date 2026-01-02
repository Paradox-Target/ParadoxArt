using CommunityToolkit.Mvvm.ComponentModel;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed partial class FocusAllowBranch(Node? trigger) : ObservableObject, IFocusTrigger
{
    [ObservableProperty]
    private bool _isEnabled;

    public Node? Trigger { get; } = trigger;
}
