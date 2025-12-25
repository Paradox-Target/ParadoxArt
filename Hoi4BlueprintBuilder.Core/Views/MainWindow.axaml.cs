using System.ComponentModel.Design;
using Avalonia;
using Avalonia.Interactivity;
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

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        double windowWidth = Width;
        double logoImageWidth = LogoImage.Width + LogoImage.Margin.Left + LogoImage.Margin.Right;
        double controlsWidth = logoImageWidth + TitleCommandBar.Bounds.Width;
        var rects = new[]
        {
            new Rect(0, 0, logoImageWidth, TitleBar.Height),
            new Rect(controlsWidth, 0, windowWidth - controlsWidth, TitleBar.Height)
        };
        TitleBar.SetDragRectangles(rects);
    }

    public MainWindow()
        : this(
            new MainWindowViewModel(new NavigationService(new ServiceContainer())),
            new NotificationService()
        ) { }
}
