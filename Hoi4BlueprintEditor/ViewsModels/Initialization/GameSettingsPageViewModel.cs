using System.Diagnostics;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views.Initialization;
using Microsoft.Win32;

namespace Hoi4BlueprintEditor.ViewsModels.Initialization;

[RegisterTransient<GameSettingsPageViewModel>]
public sealed partial class GameSettingsPageViewModel(SettingsService settings) : ObservableObject
{
    public Frame? Frame { get; set; }

    private bool IsCompleted =>
        !string.IsNullOrEmpty(GamePath) && !string.IsNullOrEmpty(ModPath) && GamePath != ModPath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    private string _gamePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    private string _modPath = string.Empty;

    [RelayCommand]
    private void PickGamePath()
    {
        var dialog = new OpenFolderDialog { Multiselect = false };

        if (dialog.ShowDialog() == true)
        {
            GamePath = dialog.FolderName;
            settings.GameRootFolderPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void PickModPath()
    {
        var dialog = new OpenFolderDialog { Multiselect = false };

        if (dialog.ShowDialog() == true)
        {
            ModPath = dialog.FolderName;
            settings.ModRootFolderPath = dialog.FolderName;
        }
    }

    [RelayCommand(CanExecute = nameof(IsCompleted))]
    private void GoToNextPage()
    {
        Debug.Assert(Frame is not null);

        if (Frame.CanGoForward)
        {
            Frame.GoForward();
        }
        else
        {
            Frame.Navigate(new AppSettingsPageView(Frame));
        }
    }
}
