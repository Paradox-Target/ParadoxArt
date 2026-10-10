using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Initialization;

namespace Hoi4BlueprintBuilder.Core.Views.Android;

[RegisterSingleton<MainAndroidView>]
public partial class MainAndroidView : UserControl
{
    public MainAndroidView(NavigationService navigationService, SettingsService settingsService)
    {
        InitializeComponent();
        navigationService.ViewChanged += currentView =>
        {
            if (currentView is not null)
            {
                Content = currentView;
            }
        };
        if (settingsService.IsFirstRun)
        {
            navigationService.NavigateTo<MainWelcomeView>();
        }
        else
        {
            navigationService.NavigateTo<ProjectListView>();
        }
        // notificationService.Initialize(
        //     TopLevel.GetTopLevel(this) ?? throw new InvalidOperationException("TopLevel is null")
        // );
    }
}
