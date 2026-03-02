using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed partial class FocusAllowBranch(IConditionExpression? expression)
    : ObservableObject,
        IFocusTrigger
{
    [ObservableProperty]
    private bool _isEnabled;

    public IConditionExpression? Expression { get; } = expression;
}
