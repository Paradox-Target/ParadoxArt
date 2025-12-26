using CommunityToolkit.Mvvm.ComponentModel;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed partial class FocusOffset(FocusPoint offset, Node? trigger) : ObservableObject
{
    public string DisplayContent => Trigger?.ToScript().TrimEnd('\n') ?? string.Empty;
    public FocusPoint Offset { get; } = offset;
    public Node? Trigger { get; } = trigger;

    [ObservableProperty]
    private bool _enabled;
}
