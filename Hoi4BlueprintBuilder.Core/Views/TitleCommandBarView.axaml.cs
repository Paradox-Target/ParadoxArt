using Avalonia.Controls;
using Avalonia.Input;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Microsoft.Extensions.DependencyInjection;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class TitleCommandBarView : UserControl
{
    public TitleCommandBarView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetRequiredService<TitleCommandBarViewModel>();
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Border { Child: Grid grid })
        {
            return;
        }

        var checkBox = grid.Children.AsValueEnumerable().OfType<CheckBox>().First();
        checkBox.IsChecked = !checkBox.IsChecked;
    }
}
