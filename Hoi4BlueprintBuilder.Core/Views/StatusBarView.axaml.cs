using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<StatusBarView>]
public sealed partial class StatusBarView : UserControl
{
    public StatusBarView(StatusBarViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
