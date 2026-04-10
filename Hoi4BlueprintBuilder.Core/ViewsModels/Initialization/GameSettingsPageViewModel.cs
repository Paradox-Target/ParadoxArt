using System.Diagnostics;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Initialization;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Initialization;

[RegisterTransient<GameSettingsPageViewModel>]
public sealed partial class GameSettingsPageViewModel(
    SettingsService settings,
    FileService fileService,
    MessageBoxService messageBoxService
) : ObservableObject
{
    public Frame? Frame { get; set; }

    private bool IsCompleted => !string.IsNullOrEmpty(GamePath) && Directory.Exists(GamePath);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    private string _gamePath = string.Empty;

    [RelayCommand]
    private async Task PickGamePath()
    {
        using var storageFolder = await fileService.OpenFolderAsync();

        if (storageFolder is not null)
        {
            //TODO: 如果要新增其他平台支持, 这一块需要改
            bool isExist = await storageFolder.GetFileAsync("hoi4.exe") is not null;
            if (!isExist)
            {
                await messageBoxService
                    .ShowErrorAsync("未在选择目录中找到 hoi4.exe 文件, 请确认当前目录是游戏根目录")
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
