using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<AppUpdateView>]
public sealed partial class AppUpdateView : UserControl
{
    public AppUpdateView()
        : this(
            new AppUpdateViewModel(null!, null!, null!, null!, null!)
            {
                HasUpdates = true,
                NewVersionText = "新版本: 1.0.0"
            }
        ) { }

    public AppUpdateView(AppUpdateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
