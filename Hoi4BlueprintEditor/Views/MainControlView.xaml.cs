using Hoi4BlueprintEditor.ViewsModels;

namespace Hoi4BlueprintEditor.Views;

[RegisterSingleton<MainControlView>]
public sealed partial class MainControlView
{
    public MainControlView(MainControlViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}