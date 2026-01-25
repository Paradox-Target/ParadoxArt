using System.Diagnostics;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Initialization;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Initialization;

[RegisterTransient<GameSettingsPageViewModel>]
public sealed partial class GameSettingsPageViewModel(SettingsService settings, FileService fileService)
    : ObservableObject
{
    public Frame? Frame { get; set; }

    private bool IsCompleted => !string.IsNullOrEmpty(GamePath) && Directory.Exists(GamePath);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    private string _gamePath = string.Empty;

    [RelayCommand]
    private async Task PickGamePath()
    {
        var storageFolder = await fileService.OpenFolderAsync();

        if (storageFolder is not null)
        {
            GamePath = storageFolder.TryGetLocalPath() ?? throw new NullReferenceException();
            settings.GameRootFolderPath = GamePath;
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
            Frame.NavigateFromObject(new AppSettingsPageView(Frame));
        }
    }
}
