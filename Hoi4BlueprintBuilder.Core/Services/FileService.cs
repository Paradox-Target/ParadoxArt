using Avalonia.Platform.Storage;
using Hoi4BlueprintBuilder.Core.Views;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<FileService>]
public sealed class FileService(MainWindow mainWindow)
{
    public async Task<IStorageFile?> OpenFileAsync(string title = "Open File")
    {
        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = title, AllowMultiple = false }
        );

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFolder?> OpenFolderAsync(string title = "Open Folder")
    {
        var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title, AllowMultiple = false }
        );

        return folders.Count >= 1 ? folders[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync(string title)
    {
        return await mainWindow.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions { Title = title }
        );
    }

    public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options)
    {
        return await mainWindow.StorageProvider.SaveFilePickerAsync(options);
    }

    public Task<bool> LaunchUriAsync(string path)
    {
        return mainWindow.Launcher.LaunchUriAsync(new Uri(path));
    }
}
