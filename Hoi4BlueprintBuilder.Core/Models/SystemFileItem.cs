using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Localization.Strings;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Models;

public sealed partial class SystemFileItem
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
    private readonly ObservableCollection<SystemFileItem> _children = [];

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

    public SystemFileItem(string fullPath, bool isFile, SystemFileItem? parent)
    {
        Name = Path.GetFileName(fullPath);
        FullPath = fullPath;
        IsFile = isFile;
        Parent = parent;
    }

    /// <summary>
    /// 添加子节点
    /// </summary>
    /// <param name="child">添加的子节点</param>
    /// <exception cref="ArgumentException">如果子节点的父节点不是当前节点, 则抛出此异常</exception>
    public void AddChild(SystemFileItem child)
    {
        if (!ReferenceEquals(child.Parent, this))
        {
            throw new ArgumentException("Child's parent should be this");
        }

        _children.Add(child);
    }

    public void AddChild(string fullPath, bool isFile)
    {
        AddChild(new SystemFileItem(fullPath, isFile, this));
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
        var dialog = new ContentDialog
        {
            Title = LangResources.Common_Rename,
            PrimaryButtonText = LangResources.Common_Ok,
            CloseButtonText = LangResources.Common_Cancel,
        };

        var view = new RenameFileView(dialog, this);
        dialog.Content = view;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
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
            await MessageBoxService.ShowAsync(LangResources.RenameFile_ErrorOccurs);
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
        NotificationService.Show("已复制到剪切板", "成功");
    }

    [RelayCommand]
    private async Task CopyAsRelativePath()
    {
        string relativePath = Path.GetRelativePath(AppSettingService.ModRootFolderPath, FullPath);
        await CopyToClipboard(relativePath).ConfigureAwait(false);
        NotificationService.Show("已复制到剪切板", "成功");
    }

    private static Task CopyToClipboard(string path)
    {
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
    }
}
