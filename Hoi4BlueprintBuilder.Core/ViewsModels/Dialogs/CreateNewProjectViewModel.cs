using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;
using Hoi4BlueprintBuilder.Core.Infrastructure.Attributes;
using Hoi4BlueprintBuilder.Localization.Strings;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

public sealed partial class CreateNewProjectViewModel : ObservableValidator
{
    public string FinalFolder => Path.Combine(Hoi4DocumentsModPath, FolderName);

    [ObservableProperty]
    [Required]
    [NotifyDataErrorInfo]
    private string _modName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalFolder))]
    [Required]
    [ValidDirectoryName]
    // 因为还需要充当描述文件的文件名称, 所以也需要按文件的标准检查
    [ValidFileName]
    [CustomValidation(typeof(CreateNewProjectViewModel), nameof(DirectoryDoesNotExistValidation))]
    [NotifyDataErrorInfo]
    private string _folderName = string.Empty;

    [ObservableProperty]
    [Required]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(CreateNewProjectViewModel), nameof(IsValidVersion))]
    private string _supportedVersion = string.Empty;

    private static readonly string Hoi4DocumentsModPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Paradox Interactive",
        "Hearts of Iron IV",
        "mod"
    );

    public CreateNewProjectViewModel()
    {
        ValidateAllProperties();
    }

    public static ValidationResult? DirectoryDoesNotExistValidation(
        string folderName,
        ValidationContext context
    )
    {
        var viewModel = (CreateNewProjectViewModel)context.ObjectInstance;
        return Directory.Exists(viewModel.FinalFolder)
            ? new ValidationResult(ZString.Format(LangResources.File_NameAlreadyExists, folderName))
            : ValidationResult.Success;
    }

    public static ValidationResult? IsValidVersion(string version, ValidationContext context)
    {
        // Version 类不支持通配符 '*', 将其全部替换为 '0'
        version = version.Replace('*', '0');
        return Version.TryParse(version, out _) ? ValidationResult.Success : new ValidationResult("错误的版本号格式");
    }
}
