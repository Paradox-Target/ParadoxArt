using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<ActivateView>]
public sealed partial class ActivateView : UserControl
{
    public ActivateView(ActivateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
