using System.Globalization;
using Hoi4BlueprintBuilder.Core.Models;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class LanguageHelper
{
    public static LanguageInfo[] AppLanguages => [new("简体中文", "zh-CN"), new("English", "en-US")];

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

    public static GameLanguage GetGameLanguageBySystemLanguage()
    {
        var cultureInfo = CultureInfo.CurrentUICulture;
        string name = cultureInfo.Name;

        if (name.StartsWith("zh"))
        {
            return GameLanguage.Chinese;
        }
        if (name.StartsWith("es"))
        {
            return GameLanguage.Spanish;
        }
        if (name.StartsWith("de"))
        {
            return GameLanguage.German;
        }
        if (name.StartsWith("ja"))
        {
            return GameLanguage.Japanese;
        }
        if (name.StartsWith("fr"))
        {
            return GameLanguage.French;
        }
        if (name.StartsWith("ru"))
        {
            return GameLanguage.Russian;
        }
        if (name.Contains("pt-BR"))
        {
            return GameLanguage.Portuguese;
        }
        if (name.StartsWith("pl"))
        {
            return GameLanguage.Polish;
        }

        return GameLanguage.English;
    }
}
