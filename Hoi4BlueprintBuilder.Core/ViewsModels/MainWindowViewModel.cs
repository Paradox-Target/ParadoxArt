using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterSingleton<MainWindowViewModel>]
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.ViewChanged += () =>
        {
            CurrentView = _navigationService.CurrentView;
        };

#if DEBUG
        navigationService.NavigateTo<MainView>();
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
}
