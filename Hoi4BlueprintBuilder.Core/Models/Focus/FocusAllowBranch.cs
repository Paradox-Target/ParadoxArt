using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed partial class FocusAllowBranch(IConditionExpression? expression)
    : ObservableObject,
        IFocusTrigger
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }
    public IConditionExpression? Expression { get; } = expression;
}
