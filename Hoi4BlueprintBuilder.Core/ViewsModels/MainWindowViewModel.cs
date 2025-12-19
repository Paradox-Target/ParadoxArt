using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Initialization;

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

        _navigationService.NavigateTo<MainWelcomeView>();
    }
}
