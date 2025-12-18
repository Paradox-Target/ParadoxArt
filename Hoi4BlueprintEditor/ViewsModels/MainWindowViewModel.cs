using System.Windows;
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

#if DEBUG
        navigationService.NavigateTo<MainControlView>();
#else
        if (App.Current.IsActivated.IsCompletedSuccessfully && App.Current.IsActivated.Result)
        {
            if (settingsService.IsFirstRun)
            {
                navigationService.NavigateTo<MainWelcomeView>();
            }
            else
            {
                if (
                    Directory.Exists(settingsService.GameRootFolderPath)
                    && Directory.Exists(settingsService.ModRootFolderPath)
                )
                {
                    navigationService.NavigateTo<MainControlView>();
                }
                else
                {
                    MessageBox.Show(
                        "游戏或MOD文件夹路径无效，请重新设置",
                        "路径无效",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    navigationService.NavigateTo<MainWelcomeView>();
                }
            }
        }
        else
        {
            navigationService.NavigateTo<LoadingView>();
        }
#endif
    }
}
