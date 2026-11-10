using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Core.Views.Android;
using Hoi4BlueprintBuilder.Localization.Strings;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<FileService>]
public sealed class FileService(IServiceProvider provider, TelemetryService telemetryService)
{
    private readonly Control _mainView = OperatingSystem.IsDesktop
        ? provider.GetRequiredService<MainWindow>()
        : provider.GetRequiredService<MainAndroidView>();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task<IStorageFile?> OpenFileAsync([LocalizationRequired] string title = "Open File")
    {
        return OpenFileAsync(new FilePickerOpenOptions { Title = title, AllowMultiple = false });
    }

    public async Task<IStorageFile?> OpenFileAsync(FilePickerOpenOptions options)
    {
        var topLevel = TopLevel.GetTopLevel(_mainView);
        if (topLevel is null)
        {
            Log.Warn("无法获取顶层窗口, 无法打开文件选择器");
            return null;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFolder?> OpenFolderAsync([LocalizationRequired] string? title = null)
    {
        title ??= LangResources.FileService_OpenFolder;
        var topLevel = TopLevel.GetTopLevel(_mainView);
        if (topLevel is null)
        {
            Log.Warn("无法获取顶层窗口, 无法打开文件夹选择器");
            return null;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title, AllowMultiple = false }
        );

        return folders.Count >= 1 ? folders[0] : null;
    }

    public async Task<string?> OpenFolderLocalPathAsync([LocalizationRequired] string? title = null)
    {
        title ??= LangResources.FileService_OpenFolder;

        var folder = await OpenFolderAsync(title).ConfigureAwait(false);
        if (OperatingSystem.IsDesktop)
        {
            return folder?.TryGetLocalPath();
        }

        if (OperatingSystem.IsAndroid())
        {
            var androidFileService = provider.GetRequiredService<IAndroidFileService>();
            return folder is not null ? androidFileService.TryGetLocalPath(folder.Path) : null;
        }

        return null;
    }

    public async Task<IStorageFile?> SaveFileAsync(string title)
    {
        var topLevel = TopLevel.GetTopLevel(_mainView);
        if (topLevel is null)
        {
            Log.Warn("无法获取顶层窗口, 无法保存文件");
            return null;
        }
        return await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions { Title = title }
        );
    }

    public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options)
    {
        var topLevel = TopLevel.GetTopLevel(_mainView);
        if (topLevel is null)
        {
            Log.Warn("无法获取顶层窗口, 无法保存文件");
            return null;
        }
        return await topLevel.StorageProvider.SaveFilePickerAsync(options);
    }

    public Task<bool> LaunchUriAsync(string path)
    {
        var topLevel = TopLevel.GetTopLevel(_mainView);
        if (topLevel is null)
        {
            Log.Warn("无法获取顶层窗口, 无法启动URI");
            return Task.FromResult(false);
        }

        return topLevel.Launcher.LaunchUriAsync(new Uri(path));
    }

    public Task<bool> LaunchUriAsync(Uri uri)
    {
        var topLevel = TopLevel.GetTopLevel(_mainView);
        if (topLevel is null)
        {
            Log.Warn("无法获取顶层窗口, 无法启动URI");
            return Task.FromResult(false);
        }

        return topLevel.Launcher.LaunchUriAsync(uri);
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
            // TODO: Android 实现
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
