namespace Hoi4BlueprintEditor.Extensions;

public static class StringExtensions
{
    public static bool EqualsIgnoreCase(this string str1, string str2)
    {
        return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
    }
}
