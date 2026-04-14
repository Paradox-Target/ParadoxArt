using System.ComponentModel.Design;
using Avalonia;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Windowing;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<MainWindow>]
public sealed partial class MainWindow : FAAppWindow
{
    public MainWindow(
        MainWindowViewModel mainWindowViewModel,
        NotificationService notificationService,
        WindowSettingsService windowSettingsService,
        SettingsService settingsService
    )
    {
        InitializeComponent();
        TitleBar.ExtendsContentIntoTitleBar = true;

        windowSettingsService.SetWindow(this);
        DataContext = mainWindowViewModel;
        notificationService.Initialize(this);

        TitleCommandBar
            .GetPropertyChangedObservable(BoundsProperty)
            .AddClassHandler<TitleCommandBarView>((_, _) => SetDragRectangles());
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            windowSettingsService.SaveWindow(this);
            windowSettingsService.SaveSettings();
            settingsService.SaveSettings();
        };
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SetDragRectangles();
    }

    private void SetDragRectangles()
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
            new MainWindowViewModel(new NavigationService(new ServiceContainer()), new SettingsService()),
            new NotificationService(),
            WindowSettingsService.LoadSettings(),
            SettingsService.LoadSettings()
        ) { }
}
