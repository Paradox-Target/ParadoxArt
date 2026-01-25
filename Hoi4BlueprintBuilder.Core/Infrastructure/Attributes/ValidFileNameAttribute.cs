using System.Buffers;
using System.ComponentModel.DataAnnotations;
using Hoi4BlueprintBuilder.Localization.Strings;

namespace Hoi4BlueprintBuilder.Core.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ValidFileNameAttribute : ValidationAttribute
{
    private static readonly SearchValues<char> InvalidCharsSearch = SearchValues.Create(
        Path.GetInvalidFileNameChars()
    );
    private static readonly SearchValues<string> ReservedFileNamesSearch = SearchValues.Create(
        [
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        ],
        StringComparison.OrdinalIgnoreCase
    );

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str[0] == ' ' || str[^1] == ' ')
            {
                return new ValidationResult(LangResources.RenameFile_InvalidFileOrFolderName);
            }
            if (str.ContainsAny(InvalidCharsSearch) || IsReservedFileNames(str))
            {
                return new ValidationResult(LangResources.RenameFile_NameContainInvalidChar);
            }
            return ValidationResult.Success;
        }

        throw new InvalidOperationException("ValidFileNameAttribute can only be used on string properties.");
    }

    private static bool IsReservedFileNames(string fileName)
    {
        if (OperatingSystem.IsWindows())
        {
            return ReservedFileNamesSearch.Contains(Path.GetFileNameWithoutExtension(fileName));
        }

        return false;
    }
}
