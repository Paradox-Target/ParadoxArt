using System.Windows;
using Hoi4BlueprintEditor.ViewModels;

namespace Hoi4BlueprintEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}