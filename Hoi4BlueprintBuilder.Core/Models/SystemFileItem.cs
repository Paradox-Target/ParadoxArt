using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Hoi4BlueprintBuilder.Localization.Strings;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Models;

public sealed partial class SystemFileItem : ObservableObject
{
    /// <summary>
    /// 当是文件时是文件名, 文件夹时是文件夹名
    /// </summary>
    public string Name { get; }

    public string FullPath { get; }
    public bool IsFile { get; }
    public bool IsFolder => !IsFile;
    public SystemFileItem? Parent { get; }
    public IReadOnlyList<SystemFileItem> Children => _children;
    private readonly AvaloniaList<SystemFileItem> _children = [];

    [ObservableProperty]
    private bool _isExpanded;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly MessageBoxService MessageBoxService =
        App.Current.Services.GetRequiredService<MessageBoxService>();
    private static readonly ClipboardService ClipboardService =
        App.Current.Services.GetRequiredService<ClipboardService>();
    private static readonly FileService FileService = App.Current.Services.GetRequiredService<FileService>();
    private static readonly SettingsService AppSettingService =
        App.Current.Services.GetRequiredService<SettingsService>();
    private static readonly NotificationService NotificationService =
        App.Current.Services.GetRequiredService<NotificationService>();
    private static readonly TelemetryService TelemetryService =
        App.Current.Services.GetRequiredService<TelemetryService>();

    private const string DefaultNewFileName = "new_file.txt";
    private const string DefaultNewFolderName = "new_folder";

    public SystemFileItem(string fullPath, bool isFile, SystemFileItem? parent)
    {
        Name = Path.GetFileName(fullPath);
        FullPath = fullPath;
        IsFile = isFile;
        Parent = parent;
    }

    /// <summary>
    /// 从文件路径创建一个文件节点
    /// </summary>
    /// <param name="fullPath">文件路径</param>
    /// <returns></returns>
    public static SystemFileItem FromFilePath(string fullPath)
    {
        return new SystemFileItem(fullPath, true, null);
    }

    /// <summary>
    /// 添加子节点
    /// </summary>
    /// <param name="child">添加的子节点</param>
    /// <exception cref="ArgumentException">如果子节点的父节点不是当前节点, 则抛出此异常</exception>
    private void AddChild(SystemFileItem child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));

        _children.Add(child);
    }

    public SystemFileItem AddChild(string fullPath, bool isFile)
    {
        var item = new SystemFileItem(fullPath, isFile, this);
        AddChild(item);
        return item;
    }

    public void AddFileChildren(IEnumerable<string> files)
    {
        _children.AddRange(files.Select(child => new SystemFileItem(child, true, this)));
    }

    public void InsertChild(int index, SystemFileItem child)
    {
        if (!ReferenceEquals(child.Parent, this))
        {
            throw new ArgumentException("Child's parent should be this");
        }

        Dispatcher.UIThread.Post(() => _children.Insert(index, child));
    }

    public void RemoveChild(SystemFileItem child)
    {
        Dispatcher.UIThread.Post(() => _children.Remove(child));
    }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(FullPath)}: {FullPath}, {nameof(IsFile)}: {IsFile}, {nameof(Children)}: {Children}";
    }

    [RelayCommand]
    private void ShowInExplorer()
    {
        FileService.OpenInExplorer(FullPath);
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        TelemetryService.TrackEvent("FileTree_ContextMenu_Rename");

        var dialog = new FAContentDialog
        {
            Title = LangResources.Common_Rename,
            PrimaryButtonText = LangResources.Common_Ok,
            CloseButtonText = LangResources.Common_Cancel,
        };

        var view = new RenameFileView(dialog, this);
        dialog.Content = view;

        var result = await dialog.ShowAsync();
        if (result != FAContentDialogResult.Primary)
        {
            Log.Debug("取消重命名");
            return;
        }

        if (view.IsInvalid || view.NewName == Name)
        {
            return;
        }

        string? parentDir = Path.GetDirectoryName(FullPath);
        if (parentDir is null)
        {
            Log.Warn("重命名文件失败，无法获取路径：{FullPath}", FullPath);
            return;
        }

        string newPath = Path.Combine(parentDir, view.NewName);
        if (Path.Exists(newPath))
        {
            Log.Warn("重命名失败，目标文件或文件夹已存在：{FullPath}", FullPath);
            return;
        }

        try
        {
            Rename(newPath);
        }
        catch (Exception e)
        {
            Log.Error(e, "重命名文件或文件夹时发生错误");
            await MessageBoxService.ShowErrorAsync(LangResources.RenameFile_ErrorOccurs);
        }
    }

    private void Rename(string newPath)
    {
        if (IsFile)
        {
            File.Move(FullPath, newPath);
        }
        else
        {
            Directory.Move(FullPath, newPath);
        }
    }

    [RelayCommand]
    private async Task CopyPath()
    {
        await CopyToClipboard(FullPath).ConfigureAwait(false);
        NotificationService.Show(LangResources.CopiedToClipboard, LangResources.Success);
    }

    [RelayCommand]
    private async Task CopyAsRelativePath()
    {
        string relativePath = Path.GetRelativePath(AppSettingService.ModRootFolderPath, FullPath);
        await CopyToClipboard(relativePath).ConfigureAwait(false);
        NotificationService.Show(LangResources.CopiedToClipboard, LangResources.Success);
    }

    private static Task CopyToClipboard(string path)
    {
        TelemetryService.TrackEvent("FileTree_ContextMenu_CopyToClipboard");
        return ClipboardService.SetTextAsync(path);
    }

    [RelayCommand]
    private async Task DeleteFile()
    {
        string text = IsFile
            ? ZString.Format(LangResources.DeleteFile_EnsureFile, Name)
            : ZString.Format(LangResources.DeleteFile_EnsureFolder, Name);
        text += $"\n\n{LangResources.DeleteFile_CanFindBack}";

        var result = await MessageBoxService.ShowAsync(
            text,
            LangResources.Common_Delete,
            MessageBoxIcon.Info,
            MessageBoxButtons.YesNo
        );

        if (result == MessageBoxResult.Yes)
        {
            if (FileService.TryMoveToRecycleBin(FullPath, out string? errorMessage))
            {
                Parent?._children.Remove(this);
            }
            else
            {
                await MessageBoxService.ShowAsync(
                    $"{LangResources.DeleteFile_Failed}{errorMessage}",
                    LangResources.Common_Error,
                    MessageBoxIcon.Error
                );
                Log.Warn("删除文件或文件夹失败：{FullPath}, 错误信息: {ErrorMessage}", FullPath, errorMessage);
            }
        }

        TelemetryService.TrackEvent("FileTree_ContextMenu_DeleteFile");
    }

    [RelayCommand]
    private async Task NewFileAsync()
    {
        TelemetryService.TrackEvent("FileTree_ContextMenu_NewFile");

        string targetDir = IsFolder ? FullPath : Path.GetDirectoryName(FullPath)!;

        var dialog = new FAContentDialog
        {
            Title = LangResources.Menu_NewFile,
            PrimaryButtonText = LangResources.Common_Ok,
            CloseButtonText = LangResources.Common_Cancel,
            DefaultButton = FAContentDialogButton.Primary
        };

        var view = new NewFileOrFolderView(dialog, targetDir, DefaultNewFileName, true);
        dialog.Content = view;

        var result = await dialog.ShowAsync();
        if (result != FAContentDialogResult.Primary)
        {
            return;
        }

        if (view.IsInvalid)
        {
            return;
        }

        try
        {
            string newFilePath = Path.Combine(targetDir, view.NewName);
            if (FileCheckHelper.IsFocusTreeFile(newFilePath))
            {
                await App
                    .Current.Services.GetRequiredService<TitleCommandBarViewModel>()
                    .CreateNewFocusTreeFileAsync(view.NewName);
            }
            else
            {
                await using (File.Create(newFilePath)) { }
            }

            if (IsFolder)
            {
                IsExpanded = true;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "新建文件时发生错误");
            await MessageBoxService.ShowErrorAsync(LangResources.NewFile_ErrorOccurs);
        }
    }

    [RelayCommand]
    private async Task NewFolderAsync()
    {
        TelemetryService.TrackEvent("FileTree_ContextMenu_NewFolder");

        string targetDir = IsFolder ? FullPath : Path.GetDirectoryName(FullPath)!;

        var dialog = new FAContentDialog
        {
            Title = LangResources.Menu_NewFolder,
            PrimaryButtonText = LangResources.Common_Ok,
            CloseButtonText = LangResources.Common_Cancel,
            DefaultButton = FAContentDialogButton.Primary
        };

        var view = new NewFileOrFolderView(dialog, targetDir, DefaultNewFolderName, false);
        dialog.Content = view;

        var result = await dialog.ShowAsync();
        if (result != FAContentDialogResult.Primary)
        {
            return;
        }

        if (view.IsInvalid)
        {
            return;
        }

        try
        {
            string newFolderPath = Path.Combine(targetDir, view.NewName);
            Directory.CreateDirectory(newFolderPath);

            if (IsFolder)
            {
                IsExpanded = true;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "新建文件夹时发生错误");
            await MessageBoxService.ShowErrorAsync(LangResources.NewFile_ErrorOccurs);
        }
    }
}
