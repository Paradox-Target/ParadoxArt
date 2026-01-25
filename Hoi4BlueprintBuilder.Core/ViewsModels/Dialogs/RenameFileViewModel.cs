using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Infrastructure.Attributes;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Localization.Strings;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

public sealed partial class RenameFileViewModel : ObservableValidator
{
    [CustomValidation(typeof(RenameFileViewModel), nameof(ValidateName))]
    [ValidFileName]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string _newName;

    public int SelectionEnd { get; }

    private readonly ContentDialog _dialog;
    private readonly SystemFileItem _fileItem;

    public RenameFileViewModel(ContentDialog dialog, SystemFileItem fileItem)
    {
        _dialog = dialog;
        _fileItem = fileItem;
        NewName = fileItem.Name;
        if (fileItem.IsFile)
        {
            int length = fileItem.Name.IndexOf('.');
            SelectionEnd = length == -1 ? 0 : length;
        }

        ErrorsChanged += (_, _) => _dialog.IsPrimaryButtonEnabled = !HasErrors;
    }

    public static ValidationResult? ValidateName(string name, ValidationContext context)
    {
        var instance = (RenameFileViewModel)context.ObjectInstance;

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
        if (_fileItem.Parent is null)
        {
            return false;
        }

        return _fileItem
            .Parent.Children.AsValueEnumerable()
            .Any(item => !ReferenceEquals(item, _fileItem) && item.Name == NewName);
    }
}
