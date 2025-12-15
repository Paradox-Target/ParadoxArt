using Hoi4BlueprintBuilder.Core.Models;

namespace Hoi4BlueprintBuilder.Core.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// 将 <see cref="GameLanguage"/> 转换为游戏本地化语言文件夹名称
    /// </summary>
    /// <param name="language"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
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
