using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed class FocusOffset(FocusPoint offset, Node? trigger)
{
    public FocusPoint Offset { get; } = offset;
    public Node? Trigger { get; } = trigger;
}
