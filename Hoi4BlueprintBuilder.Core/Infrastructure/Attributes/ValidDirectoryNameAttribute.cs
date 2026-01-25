using System.Buffers;
using System.ComponentModel.DataAnnotations;
using Hoi4BlueprintBuilder.Localization.Strings;

namespace Hoi4BlueprintBuilder.Core.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ValidDirectoryNameAttribute : ValidationAttribute
{
    private static readonly SearchValues<char> InvalidPathCharsSearch = SearchValues.Create(
        Path.GetInvalidPathChars()
    );

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string str)
        {
            throw new InvalidOperationException(
                "ValidDirectoryNameAttribute can only be used on string properties."
            );
        }

        if (string.IsNullOrWhiteSpace(str) || str[0] == ' ' || str[^1] == ' ')
        {
            return new ValidationResult(LangResources.RenameFile_InvalidFileOrFolderName);
        }

        if (str.ContainsAny(InvalidPathCharsSearch))
        {
            return new ValidationResult(LangResources.RenameFile_NameContainInvalidChar);
        }

        return ValidationResult.Success;
    }
}
