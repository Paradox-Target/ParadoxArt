namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public interface IFocusTrigger
{
    bool IsEnabled { get; set; }
    IConditionExpression? Expression { get; }
}
