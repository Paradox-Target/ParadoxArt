using System.Globalization;

namespace Hoi4BlueprintEditor.Core;

public interface ILocalizationService
{
    string GetString(string key);

    string GetString(string key, CultureInfo culture);
}
