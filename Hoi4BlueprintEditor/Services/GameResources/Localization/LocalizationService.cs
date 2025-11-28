using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Services.GameResources.Base;
using MethodTimer;
using ParadoxPower.CSharp;
using ParadoxPower.Localisation;

namespace Hoi4BlueprintEditor.Services.GameResources.Localization;

[RegisterSingleton<LocalizationService>]
public sealed class LocalizationService
    : ResourcesService<LocalizationService, FrozenDictionary<string, string>, YAMLLocalisationParser.LocFile>
{
    private ICollection<FrozenDictionary<string, string>> Localisations => Resources.Values;

    /// Key: 国策文件路径, Value: 国策文件的本地化内容, 键值对
    private readonly Dictionary<
        (string FilePath, GameLanguage),
        Dictionary<string, string>
    > _filesLocalisations = new();

    [Time("加载本地化文件")]
    public LocalizationService(SettingsService settingsService, IServiceProvider serviceProvider)
        : base(
            Path.Combine("localisation", settingsService.GameLanguage.ToGameLocalizationLanguage()),
            WatcherFilter.LocalizationFiles,
            serviceProvider,
            PathType.Folder,
            SearchOption.AllDirectories,
            true
        )
    {
        WeakReferenceMessenger.Default.Register<SaveFocusTreeMessage>(
            this,
            (_, _) =>
            {
                foreach (
                    ((string focusFilePath, var gameLanguage), var userLocalisation) in _filesLocalisations
                )
                {
                    // userLocalisation => Key: 本地化键, Value: 本地化文本
                    string filePath = Path.Combine(
                        settingsService.ModRootFolderPath,
                        "localisation",
                        gameLanguage.ToGameLocalizationLanguage(),
                        $"{Path.GetFileNameWithoutExtension(focusFilePath)}.yml"
                    );
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                    YAMLLocalisationParser.LocFile? result;
                    if (File.Exists(filePath) && (result = GetParseResult(filePath)) is not null)
                    {
                        var currentLocalisation = result.Entries.ToDictionary(
                            static entry => entry.Key,
                            static entry => entry.Desc,
                            StringComparer.OrdinalIgnoreCase
                        );
                        foreach (var content in userLocalisation)
                        {
                            currentLocalisation[content.Key] = content.Value;
                        }
                        WriteLocalisationToFile(filePath, $"{result.Key}:", currentLocalisation);
                    }
                    else
                    {
                        string key = GameLanguageToGameLocalizationKey(settingsService.GameLanguage);
                        WriteLocalisationToFile(filePath, key, userLocalisation);
                    }

                    Log.Info("成功保存本地化文件: {FilePath}", filePath);
                }

                // TODO: 有可能会有一段时间的真空期, 解决方案: 监听文件更改, 当文件更改时才清空?
                _filesLocalisations.Clear();
            }
        );
    }

    private static void WriteLocalisationToFile(
        string filePath,
        string languageKey,
        Dictionary<string, string> localisation
    )
    {
        Debug.Assert(languageKey.StartsWith("l_") && languageKey.EndsWith(':'), "languageKey");

        using var localisationFile = new StreamWriter(filePath, append: false, Encoding.UTF8);
        localisationFile.WriteLine(languageKey);
        foreach (var content in localisation)
        {
            // 钢丝换行只用\n
            string text = content.Value.Replace("\r\n", "\\n").Replace("\n", "\\n");
            localisationFile.WriteLine($" {content.Key}: \"{text}\"");
        }
    }

    private static string GameLanguageToGameLocalizationKey(GameLanguage language)
    {
        return language switch
        {
            GameLanguage.English => "l_english:",
            GameLanguage.German => "l_german:",
            GameLanguage.French => "l_french:",
            GameLanguage.Spanish => "l_spanish:",
            GameLanguage.Russian => "l_russian:",
            GameLanguage.Chinese => "l_simp_chinese:",
            GameLanguage.Japanese => "l_japanese:",
            GameLanguage.Portuguese => "l_braz_por:",
            GameLanguage.Polish => "l_polish:",
            _ => throw new ArgumentOutOfRangeException(nameof(language))
        };
    }

    /// <summary>
    /// 如果本地化文本不存在, 则返回<c>key</c>
    /// </summary>
    /// <returns></returns>
    public string GetValue(string key)
    {
        return TryGetValue(key, out string? value) ? value : key;
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        foreach (var filesLocalisations in _filesLocalisations.Values)
        {
            if (filesLocalisations.TryGetValue(key, out value))
            {
                return true;
            }
        }

        foreach (var localisation in Localisations)
        {
            if (localisation.TryGetValue(key, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// 添加本地化, 如果本地化已存在, 则覆盖
    /// </summary>
    /// <param name="filePath">国策文件路径</param>
    /// <param name="gameLanguage">传入的本地化的语言</param>
    /// <param name="key">键</param>
    /// <param name="value">本地化值</param>
    public void AddOrUpdateLocalisation(string filePath, GameLanguage gameLanguage, string key, string value)
    {
        var mapKey = (filePath, gameLanguage);
        if (!_filesLocalisations.TryGetValue(mapKey, out var localisation))
        {
            localisation = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _filesLocalisations.Add(mapKey, localisation);
        }
        localisation[key] = value;
    }

    protected override FrozenDictionary<string, string> ParseFileToContent(
        YAMLLocalisationParser.LocFile result
    )
    {
        var localisations = new Dictionary<string, string>(result.Entries.Count);
        foreach (var item in result.Entries)
        {
            localisations[item.Key] = item.Desc;
        }

        return localisations.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    protected override YAMLLocalisationParser.LocFile? GetParseResult(string filePath)
    {
        var localisation = YAMLLocalisationParser.ParseLocFile(filePath);
        if (localisation.IsFailure)
        {
            Log.LogParseError(localisation.GetError()!);
            return null;
        }

        var result = localisation.GetResult()!;
        return result;
    }
}
