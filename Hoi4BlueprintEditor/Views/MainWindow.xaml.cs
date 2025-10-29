using System.Windows;
using Hoi4BlueprintEditor.ViewsModels;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
