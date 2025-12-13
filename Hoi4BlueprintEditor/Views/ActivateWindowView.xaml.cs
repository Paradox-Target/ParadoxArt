using System.Windows.Controls;
using Hoi4BlueprintEditor.ViewsModels;

namespace Hoi4BlueprintEditor.Views;

[RegisterTransient<ActivateWindowView>]
public sealed partial class ActivateWindowView : UserControl
{
    public ActivateWindowView(ActivateWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
