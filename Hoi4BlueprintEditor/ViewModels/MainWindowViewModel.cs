using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views;

namespace Hoi4BlueprintEditor.ViewModels;

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
        
        navigationService.NavigateTo<MainControlView>();
    }
}
