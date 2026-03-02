using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed partial class FocusOffset(FocusPoint offset, IConditionExpression? expression)
    : ObservableObject,
        IFocusTrigger
{
    public FocusPoint Offset { get; } = offset;

    public IConditionExpression? Expression { get; } = expression;

    [ObservableProperty]
    private bool _isEnabled;
}
