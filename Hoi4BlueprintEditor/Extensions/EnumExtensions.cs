using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Models;

namespace Hoi4BlueprintEditor.Extensions;

public static class EnumExtensions
{
    public static string ToGameLocalizationLanguage(this GameLanguage language)
    {
        if (language == GameLanguage.Default)
        {
            language = LanguageHelper.GetGameLanguageBySystemLanguage();
        }

        return language switch
        {
            GameLanguage.Chinese => "simp_chinese",
            GameLanguage.English => "english",
            GameLanguage.Russian => "russian",
            GameLanguage.Spanish => "spanish",
            GameLanguage.German => "german",
            GameLanguage.French => "french",
            GameLanguage.Japanese => "japanese",
            GameLanguage.Portuguese => "braz_por",
            GameLanguage.Polish => "polish",
            GameLanguage.Default => throw new ArgumentException($"{nameof(GameLanguage.Default)} 未转换为系统本地语言"),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
    }
}
