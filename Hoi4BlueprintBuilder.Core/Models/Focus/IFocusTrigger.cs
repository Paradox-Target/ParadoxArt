using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public interface IFocusTrigger
{
    bool IsEnabled { get; set; }
    Node? Trigger { get; }
    string DisplayContent => Trigger?.ToScript().TrimEnd('\n') ?? string.Empty;
}
