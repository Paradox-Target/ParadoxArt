using System.Diagnostics;
using Avalonia.Platform.Storage;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Views;
using Microsoft.VisualBasic.FileIO;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<FileService>]
public sealed class FileService(MainWindow mainWindow, TelemetryService telemetryService)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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

    public void OpenInExplorer(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var startInfo = new ProcessStartInfo("explorer.exe")
            {
                UseShellExecute = true,
                Arguments = $"/select, \"{path}\""
            };
            using var process = Process.Start(startInfo);
        }
        else if (OperatingSystem.IsLinux())
        {
            bool isFile = File.Exists(path);
            if (isFile)
            {
                // 不打开文件, 只打开文件所属文件夹
                // TODO: 可以使用 Dolphin 或 Nautilus 直接打开文件夹并选中对应文件
                path = Path.GetDirectoryName(path) ?? path;
            }
            var startInfo = new ProcessStartInfo("xdg-open") { Arguments = path };

            using var process = Process.Start(startInfo);
        }
        else
        {
            Log.Error("无法在资源管理器中打开, 不支持的操作系统");
        }

        telemetryService.TrackEvent("OpenInExplorer");
    }

    public bool TryMoveToRecycleBin(string path, out string? message)
    {
        telemetryService.TrackEvent("TryMoveToRecycleBin");

        try
        {
            if (OperatingSystem.IsWindows())
            {
                return TryMoveToRecycleBinForWindows(path, out message);
            }

            if (OperatingSystem.IsLinux())
            {
                return LinuxFileHelper.TryMoveToRecycleBin(path, out message);
            }

            message = "平台不支持移动至回收站操作";
            return false;
        }
        catch (Exception e)
        {
            message = e.Message;
            const string errorMessage = "移动文件或文件夹到回收站时发生错误";
            telemetryService.TrackException(e, errorMessage);
            Log.Error(e, errorMessage);
            return false;
        }
    }

    private static bool TryMoveToRecycleBinForWindows(string path, out string? message)
    {
        if (File.Exists(path))
        {
            FileSystem.DeleteFile(
                path,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.DoNothing
            );
        }
        else if (Directory.Exists(path))
        {
            FileSystem.DeleteDirectory(
                path,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.DoNothing
            );
        }
        else
        {
            message = "文件或文件夹不存在";
            return false;
        }

        message = null;
        return true;
    }
}
