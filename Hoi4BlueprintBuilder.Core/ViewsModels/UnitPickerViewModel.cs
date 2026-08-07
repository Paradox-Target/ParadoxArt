using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

public sealed partial class UnitPickerViewModel : ObservableObject
{
    public IEnumerable<UnitGroupVo> Units { get; }

    [ObservableProperty]
    public partial IEnumerable<UnitInfoVo>? SubUnits { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    public partial bool IsSubUnitView { get; private set; }

    [ObservableProperty]
    public partial bool IsVisibleBackButton { get; private set; } = true;

    public UnitInfoVo? SelectedUnit { get; private set; }

    public event Action? Close;

    public UnitPickerViewModel(IReadOnlyCollection<UnitGroupVo> units)
    {
        if (units.Count == 1)
        {
            IsSubUnitView = true;
            SubUnits = units.First().Units;
            Units = [];
            IsVisibleBackButton = false;
        }
        else
        {
            Units = units;
        }
    }

    [RelayCommand]
    private void PickGroup(UnitGroupVo? group)
    {
        if (group is null)
        {
            return;
        }

        SubUnits = group.Units;
        IsSubUnitView = true;
    }

    [RelayCommand]
    private void PickSubUnit(UnitInfoVo unit)
    {
        SelectedUnit = unit;
        Close?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(IsSubUnitView))]
    private void Back()
    {
        SubUnits = null;
        IsSubUnitView = false;
    }
}
