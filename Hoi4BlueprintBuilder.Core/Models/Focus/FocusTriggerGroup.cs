using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed partial class FocusTriggerGroup(string displayContent) : ObservableObject
{
    public string DisplayContent { get; } = displayContent;

    [ObservableProperty]
    private bool _isEnabled;

    private readonly List<IFocusTrigger> _triggers = [];

    partial void OnIsEnabledChanged(bool value)
    {
        foreach (var trigger in _triggers)
        {
            trigger.IsEnabled = value;
        }
    }

    public void AddTrigger(IFocusTrigger trigger)
    {
        _triggers.Add(trigger);
    }
}
