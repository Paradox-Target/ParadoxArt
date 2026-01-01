using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Localization.Strings;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

public sealed partial class CreateNewFocusViewModel : ObservableValidator
{
    public FocusType SelectedFocusType => FocusTypes[SelectedFocusTypeIndex].Type;
    public string SelectedFocusFilePath => FocusFileNames[SelectedFocusFileNameIndex].FilePath;
    public FocusTypeItem[] FocusTypes { get; } =
        [
            new(FocusType.Normal, LangResources.CreateNewFocusView_CommonFocus),
            new(FocusType.Shared, LangResources.CreateNewFocusView_SharedFocus)
        ];
    public FocusFileItem[] FocusFileNames { get; }

    [ObservableProperty]
    [Required]
    [CustomValidation(typeof(CreateNewFocusViewModel), nameof(FocusIdShouldIsUnique))]
    [NotifyDataErrorInfo]
    private string _focusId = string.Empty;

    [ObservableProperty]
    [Range(0, int.MaxValue)]
    [NotifyDataErrorInfo]
    private int _selectedFocusTypeIndex;

    [ObservableProperty]
    [Range(0, int.MaxValue)]
    [NotifyDataErrorInfo]
    private int _selectedFocusFileNameIndex;

    private bool IsValid => SelectedFocusFileNameIndex >= 0 && !HasErrors;

    private Action<bool>? _setPrimaryEnableAction;
    private Func<string, bool>? _focusIdIsExistsFunc;

    public CreateNewFocusViewModel(
        IReadOnlyCollection<string> filePaths,
        Action<bool> setPrimaryEnableAction,
        Func<string, bool> focusIdIsExistsFunc
    )
    {
        _setPrimaryEnableAction = setPrimaryEnableAction;
        _focusIdIsExistsFunc = focusIdIsExistsFunc;
        FocusFileNames = filePaths
            .AsValueEnumerable()
            .Select(static path => new FocusFileItem(Path.GetFileName(path), path))
            .Where(item => !string.IsNullOrWhiteSpace(item.FileName))
            .ToArray();

        if (FocusFileNames.Length == 1)
        {
            _selectedFocusFileNameIndex = 0;
        }
        else
        {
            _selectedFocusFileNameIndex = -1;
        }

        PropertyChanged += (_, _) => _setPrimaryEnableAction?.Invoke(IsValid);
    }

    public static ValidationResult? FocusIdShouldIsUnique(string focusId, ValidationContext context)
    {
        var viewModel = (CreateNewFocusViewModel)context.ObjectInstance;
        return viewModel._focusIdIsExistsFunc?.Invoke(focusId) is true
            ? new ValidationResult(
                ZString.Format(LangResources.CreateNewFocusView_FocusIdAlreadyExist, focusId)
            )
            : ValidationResult.Success;
    }

    public void Clean()
    {
        _setPrimaryEnableAction = null;
        _focusIdIsExistsFunc = null;
    }
}
