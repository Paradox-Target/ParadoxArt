using Hoi4BlueprintEditor.Models;

namespace Hoi4BlueprintEditor.Extensions;

public static class EnumExtensions
{
    public static string ToGameLocalizationLanguage(this GameLanguage language)
    {
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
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
    }
}
