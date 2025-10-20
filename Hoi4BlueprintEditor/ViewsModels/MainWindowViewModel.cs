using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views;
using Hoi4BlueprintEditor.Views.Initialization;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel(NavigationService navigationService, SettingsService settingsService)
    {
        _navigationService = navigationService;
        
        _navigationService.ViewChanged += () =>
        {
            CurrentView = _navigationService.CurrentView;
        };

        if (settingsService.IsFirstRun)
        {
            navigationService.NavigateTo<MainWelcomeView>();
        }
        else
        {
            navigationService.NavigateTo<MainControlView>();
        }
    }
}
