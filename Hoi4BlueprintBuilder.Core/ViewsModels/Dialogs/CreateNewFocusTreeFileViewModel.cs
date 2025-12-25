using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;
using Hoi4BlueprintBuilder.Localization.Strings;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

public sealed partial class CreateNewFocusTreeFileViewModel : ObservableValidator
{
    public string FinalFilePath => GetFinalFilePath();
    private readonly string _focusTreeDirectory;

    [ObservableProperty]
    [Required]
    [CustomValidation(typeof(CreateNewFocusTreeFileViewModel), nameof(FileShouldNotExist))]
    [NotifyDataErrorInfo]
    private string _fileName = string.Empty;

    // TODO: 应该检查
    [ObservableProperty]
    private string _countryTag = string.Empty;

    [ObservableProperty]
    [Required]
    [NotifyDataErrorInfo]
    private string _id = string.Empty;

    [ObservableProperty]
    private bool _isDefaultFocusTree;

    public event Action<bool>? PrimaryEnableChanged;

    private bool CanCreate =>
        !string.IsNullOrWhiteSpace(FileName)
        && (!string.IsNullOrWhiteSpace(CountryTag) || IsDefaultFocusTree)
        && !string.IsNullOrWhiteSpace(Id)
        && !HasErrors;

    public CreateNewFocusTreeFileViewModel(string focusTreeDirectory)
    {
        _focusTreeDirectory = focusTreeDirectory;
        PropertyChanged += (_, _) => PrimaryEnableChanged?.Invoke(CanCreate);
        ErrorsChanged += (_, _) => PrimaryEnableChanged?.Invoke(CanCreate);
    }

    public void Clean()
    {
        PrimaryEnableChanged = null;
    }

    public static ValidationResult? FileShouldNotExist(string fileName, ValidationContext context)
    {
        var viewModel = (CreateNewFocusTreeFileViewModel)context.ObjectInstance;

        string filePath = viewModel.FinalFilePath;
        return File.Exists(filePath)
            ? new ValidationResult(
                ZString.Format(Resources.CreateNewFocusTreeFileView_FileNameAlreadyExist, fileName)
            )
            : ValidationResult.Success;
    }

    private string GetFinalFilePath()
    {
        string filePath = FileName;
        if (!filePath.EndsWith(".txt"))
        {
            filePath += ".txt";
        }

        return Path.Combine(_focusTreeDirectory, filePath);
    }
}
