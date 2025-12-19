using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<MainWindow>]
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel mainWindowViewModel, NotificationService notificationService)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;
        notificationService.Initialize(this);
    }
}
