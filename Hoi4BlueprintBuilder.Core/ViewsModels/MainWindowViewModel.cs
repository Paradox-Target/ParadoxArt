using System.ComponentModel.Design;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Core.Views.Initialization;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterSingleton<MainWindowViewModel>]
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel(NavigationService navigationService, SettingsService settingsService)
    {
        _navigationService = navigationService;
        _navigationService.ViewChanged += currentView => CurrentView = currentView;
#if DEBUG
        if (settingsService.IsFirstRun)
        {
            navigationService.NavigateTo<MainWelcomeView>();
        }
        else
        {
            navigationService.NavigateTo<MainView>();
        }
#else
        if (App.Current.IsActivated?.IsCompleted is true)
        {
            navigationService.NavigateBasedOnDeviceStatus();
        }
        else
        {
            navigationService.NavigateTo<LoadingView>();
        }
#endif
    }

    public MainWindowViewModel()
        : this(new NavigationService(new ServiceContainer()), new SettingsService()) { }
}
