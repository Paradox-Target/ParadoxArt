using FluentAvalonia.UI.Windowing;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<MainWindow>]
public sealed partial class MainWindow : AppWindow
{
    public MainWindow(MainWindowViewModel mainWindowViewModel, NotificationService notificationService)
    {
        InitializeComponent();
        TitleBar.ExtendsContentIntoTitleBar = true;

        DataContext = mainWindowViewModel;
        notificationService.Initialize(this);
    }
}
