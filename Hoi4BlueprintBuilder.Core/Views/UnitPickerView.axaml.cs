using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class UnitPickerView : UserControl
{
    public UnitPickerView()
    {
        InitializeComponent();
    }

    public UnitPickerView(UnitPickerViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
