using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.Services;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<MainWindow>]
public partial class MainWindow : Window
{
    public MainWindow(NavigationService navigationService)
    {
        InitializeComponent();

        navigationService.ViewChanged += () =>
        {
            MainContent.Content = navigationService.CurrentView;
        };
    }
}
