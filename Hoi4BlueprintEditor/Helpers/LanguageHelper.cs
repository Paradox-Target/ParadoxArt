using System.Globalization;

namespace Hoi4BlueprintEditor.Helpers;

public static class LanguageHelper
{
    public static void SetLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
        {
            return;
        }

        var culture = new CultureInfo(language);
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
