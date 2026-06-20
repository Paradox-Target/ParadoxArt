using System.Diagnostics;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Initialization;
using Hoi4BlueprintBuilder.Localization.Strings;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Initialization;

[RegisterTransient<GameSettingsPageViewModel>]
public sealed partial class GameSettingsPageViewModel(
    SettingsService settings,
    FileService fileService,
    MessageBoxService messageBoxService
) : ObservableObject
{
    public FAFrame? Frame { get; set; }

    private bool IsCompleted => !string.IsNullOrEmpty(GamePath) && Directory.Exists(GamePath);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    public partial string GamePath { get; set; } = string.Empty;

    [RelayCommand]
    private async Task PickGamePath()
    {
        using var storageFolder = await fileService.OpenFolderAsync();

        if (storageFolder is not null)
        {
            bool isExist = await FileCheckHelper.IsValidGameRootDirectoryAsync(storageFolder);
            if (!isExist)
            {
                await messageBoxService
                    .ShowErrorAsync(
                        string.Format(
                            LangResources.AppSettings_Hoi4ExeNotFound,
                            FileCheckHelper.GameExeFileName
                        )
                    )
                    .ConfigureAwait(false);
                return;
            }
            GamePath = storageFolder.TryGetLocalPath() ?? throw new InvalidOperationException();
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
