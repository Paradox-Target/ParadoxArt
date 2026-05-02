using System.ComponentModel.Design;
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

        Closed += (_, _) =>
        {
            windowSettingsService.SaveWindow(this);
            windowSettingsService.SaveSettings();
            settingsService.SaveSettings();
        };
    }

    public MainWindow()
        : this(
            new MainWindowViewModel(new NavigationService(new ServiceContainer()), new SettingsService()),
            new NotificationService(),
            WindowSettingsService.LoadSettings(),
            SettingsService.LoadSettings()
        ) { }
}
