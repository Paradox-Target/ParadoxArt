using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Infrastructure.Attributes;
using Hoi4BlueprintBuilder.Localization.Strings;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

public sealed partial class NewFileOrFolderViewModel : ObservableValidator
{
    [CustomValidation(typeof(NewFileOrFolderViewModel), nameof(ValidateName))]
    [ValidFileName]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string _newName;

    public int SelectionEnd { get; }

    private readonly FAContentDialog _dialog;
    private readonly string _targetDirectoryPath;

    /// <summary>
    /// 创建新建文件/文件夹的 ViewModel
    /// </summary>
    /// <param name="dialog">对话框实例</param>
    /// <param name="targetDirectoryPath">目标目录路径</param>
    /// <param name="defaultName">默认文件/文件夹名</param>
    /// <param name="isFile">是否为文件（影响默认选中范围）</param>
    public NewFileOrFolderViewModel(
        FAContentDialog dialog,
        string targetDirectoryPath,
        string defaultName,
        bool isFile
    )
    {
        _dialog = dialog;
        _targetDirectoryPath = targetDirectoryPath;
        NewName = defaultName;

        if (isFile)
        {
            int length = defaultName.IndexOf('.');
            SelectionEnd = length == -1 ? defaultName.Length : length;
        }
        else
        {
            SelectionEnd = defaultName.Length;
        }

        ErrorsChanged += (_, _) => _dialog.IsPrimaryButtonEnabled = !HasErrors;
    }

    public static ValidationResult? ValidateName(string name, ValidationContext context)
    {
        var instance = (NewFileOrFolderViewModel)context.ObjectInstance;

        if (instance.HasEqualsNameItem())
        {
            return new ValidationResult(
                ZString.Format(LangResources.File_NameAlreadyExists, instance.NewName)
            );
        }

        return ValidationResult.Success;
    }

    private bool HasEqualsNameItem()
    {
        string targetPath = Path.Combine(_targetDirectoryPath, NewName);
        return Path.Exists(targetPath);
    }
}
