using System.ComponentModel.DataAnnotations;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using EnumsNET;
using Hoi4BlueprintBuilder.Core.Infrastructure.Attributes;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Localization.Strings;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

public sealed partial class CreateNewProjectViewModel : ObservableValidator
{
    public string FinalFolder => Path.Combine(Hoi4DocumentsModPath, FolderName);
    public AvaloniaList<GameLanguage> SupportedLanguages { get; }

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

    // 虽然支持语言为 0 也可以, 但加个警告时不时更好？
    public bool ShowTagsErrorMessage => _tags.Count is < 1 or > 10;
    public string? TagsErrorMessage => GetTagsCountErrorMessage();
    public IEnumerable<string> Tags => _tags;

    private readonly Action<bool>? _setPrimaryEnableAction;
    private bool IsValid => !HasErrors && !ShowTagsErrorMessage;

    private readonly List<string> _tags = new(4);

    private static readonly string Hoi4DocumentsModPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Paradox Interactive",
        "Hearts of Iron IV",
        "mod"
    );

    public static readonly string[] AllTags =
    [
        "Alternative History",
        "Balance",
        "Events",
        "Fixes",
        "Gameplay",
        "Graphics",
        "Historical",
        "Ideologies",
        "Map",
        "Military",
        "National Focuses",
        "Sound",
        "Technologies",
        "Translation",
        "Utilities"
    ];

    public IReadOnlyList<GameLanguage> Languages => Enums.GetValues<GameLanguage>();

    public CreateNewProjectViewModel(SettingsService settingsService, Action<bool>? setPrimaryEnableAction)
    {
        _setPrimaryEnableAction = setPrimaryEnableAction;
        ErrorsChanged += (_, _) => UpdatePrimaryButtonState();

        ValidateAllProperties();
        SupportedLanguages = [settingsService.GameLanguage];
    }

    private void UpdatePrimaryButtonState() => _setPrimaryEnableAction?.Invoke(IsValid);

    private string? GetTagsCountErrorMessage()
    {
        return _tags.Count switch
        {
            < 1 => "至少需要选择 1 个标签",
            > 10 => "最多只能选择 10 个标签",
            _ => null
        };
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

    [RelayCommand]
    private void ChangeCurrentTags(RoutedEventArgs eventArgs)
    {
        if (eventArgs.Source is not CheckBox { Content: string tag } checkBox)
        {
            return;
        }

        if (checkBox.IsChecked == true)
        {
            _tags.Add(tag);
        }
        else
        {
            _tags.Remove(tag);
        }

        OnPropertyChanged(nameof(TagsErrorMessage));
        OnPropertyChanged(nameof(ShowTagsErrorMessage));
        UpdatePrimaryButtonState();
    }
}
