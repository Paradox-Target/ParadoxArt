using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Localization;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using MessagePipe;
using MethodTimer;
using ParadoxPower.CSharp;
using ParadoxPower.Localisation;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;

[RegisterSingleton<LocalizationService>]
public sealed class LocalizationService
    : ResourcesService<
        LocalizationService,
        (GameLanguage Language, FrozenDictionary<string, string> Items),
        YAMLLocalisationParser.LocFile
    >,
        IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly GameResourcesPathService _gameResourcesPathService;
    private readonly IDisposable _saveLocalizationSubscription;

    private ICollection<(GameLanguage Language, FrozenDictionary<string, string> Items)> Localisations =>
        Resources.Values;

    /// Key: 国策文件路径, Value: 国策文件的本地化内容, 键值对
    private readonly Dictionary<
        (string FilePath, GameLanguage Language),
        Dictionary<string, string>
    > _filesLocalisations = new();

    private static readonly (string Key, GameLanguage Enum)[] LanguageMap = Enum.GetValues<GameLanguage>()
        .AsValueEnumerable()
        .Select(gameLanguage => ($"l_{gameLanguage.ToGameLocalizationLanguage()}", gameLanguage))
        .ToArray();

    [Time("加载本地化文件")]
    public LocalizationService(
        SettingsService settingsService,
        GameResourcesPathService gameResourcesPathService,
        ProjectConfigService configService,
        IServiceProvider serviceProvider,
        ISubscriber<SaveLocalizationMessage> saveLocalizationSubscriber
    )
        : base(
            configService
                .SupportedLanguages.AsValueEnumerable()
                .Select(language => Path.Combine("localisation", language.ToGameLocalizationLanguage()))
                .ToArray(),
            WatcherFilter.LocalizationFiles,
            serviceProvider,
            PathType.Folder,
            SearchOption.AllDirectories,
            true
        )
    {
        _settingsService = settingsService;
        _gameResourcesPathService = gameResourcesPathService;
        _saveLocalizationSubscription = saveLocalizationSubscriber.Subscribe(SaveLocalizationHandler);
    }

    public void Dispose()
    {
        _saveLocalizationSubscription.Dispose();
    }

    private void SaveLocalizationHandler(SaveLocalizationMessage _)
    {
        foreach (((string path, var gameLanguage), var userLocalisation) in _filesLocalisations)
        {
            string languageKey = $"l_{gameLanguage.ToGameLocalizationLanguage()}";
            // userLocalisation => Key: 本地化键, Value: 本地化文本
            string filePath = Path.Combine(
                _settingsService.ModRootFolderPath,
                "localisation",
                gameLanguage.ToGameLocalizationLanguage(),
                GetFileName(path, languageKey)
            );
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            YAMLLocalisationParser.LocFile? result;
            if (File.Exists(filePath) && (result = GetParseResult(filePath)) is not null)
            {
                var currentLocalisation = new Dictionary<string, string>();

                foreach (var entry in result.Entries)
                {
                    currentLocalisation[entry.Key] = entry.Desc;
                }

                foreach (var content in userLocalisation)
                {
                    currentLocalisation[content.Key] = content.Value;
                }

                WriteLocalisationToFile(filePath, $"{languageKey}:", currentLocalisation);
            }
            else
            {
                WriteLocalisationToFile(filePath, $"{languageKey}:", userLocalisation);
            }

            Log.Info("成功保存本地化文件: {FilePath}", filePath);
        }

        // TODO: 有可能会有一段时间的真空期, 解决方案: 监听文件更改, 当文件更改时才清空?
        _filesLocalisations.Clear();
    }

    private static string GetFileName(string filePath, string languageKey)
    {
        if (FileCheckHelper.IsFocusTreeFile(filePath))
        {
            return $"{Path.GetFileNameWithoutExtension(filePath)}_{languageKey}.yml";
        }

        var fileName = Path.GetFileName(filePath.AsSpan());
        int index = fileName.IndexOf("_l_", StringComparison.InvariantCulture);
        if (index != -1)
        {
            return $"{fileName[..index]}_{languageKey}.yml";
        }

        return $"{Path.GetFileNameWithoutExtension(filePath)}_{languageKey}.yml";
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

    /// <summary>
    /// 如果本地化文本不存在, 则返回<c>key</c>
    /// </summary>
    /// <returns></returns>
    public string GetValue(string key)
    {
        return TryGetValue(key, out string? value) ? value : key;
    }

    /// <summary>
    /// 使用当前游戏语言尝试获取本地化文本, 如果找到返回<c>true</c>和对应的值, 反之返回<c>false</c>和<c>null</c>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        return TryGetValue(key, _settingsService.GameLanguage, out value);
    }

    public bool TryGetValue(string key, GameLanguage language, [NotNullWhen(true)] out string? value)
    {
        foreach (
            var filesLocalisation in _filesLocalisations
                .AsValueEnumerable()
                .Where(filesLocalisation => filesLocalisation.Key.Language == language)
        )
        {
            if (filesLocalisation.Value.TryGetValue(key, out value))
            {
                return true;
            }
        }

        foreach (
            var localisation in Localisations.AsValueEnumerable().Where(items => items.Language == language)
        )
        {
            if (localisation.Items.TryGetValue(key, out value))
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

    /// <summary>
    /// 获取当前 MOD 下所有本地化行数据（仅包含 Mod 目录下解析的文件和内存中暂存的内容）
    /// </summary>
    public IEnumerable<LocalizationRow> GetAllModLocalisationRows()
    {
        var rowsMap = new Dictionary<string, LocalizationRow>(StringComparer.OrdinalIgnoreCase);

        // 1. 获取 MOD 下的所有静态文件中的记录
        foreach (var resource in Resources)
        {
            string filePath = resource.Key;
            // 判定是否是在 MOD 文件夹下
            if (_gameResourcesPathService.GetFileOrigin(filePath) != FileOrigin.Mod)
            {
                continue;
            }

            var language = resource.Value.Language;
            var items = resource.Value.Items;

            foreach ((string key, string value) in items)
            {
                if (!rowsMap.TryGetValue(key, out var row))
                {
                    row = new LocalizationRow(key);
                    rowsMap[key] = row;
                }

                var langItem = row.Languages.AsValueEnumerable().FirstOrDefault(l => l.Language == language);
                if (langItem is null)
                {
                    row.Languages.Add(new LocalizationLanguageItem(language, filePath, value));
                }
                else
                {
                    langItem.Value = value;
                }
            }
        }

        // 2. 合并由用户修改而暂存到 _filesLocalisations 的内容 (优先覆盖)
        foreach (var (mapKey, localizations) in _filesLocalisations)
        {
            (string filePath, var language) = mapKey;
            foreach (var (key, value) in localizations)
            {
                if (!rowsMap.TryGetValue(key, out var row))
                {
                    row = new LocalizationRow(key);
                    rowsMap[key] = row;
                }

                var langItem = row.Languages.AsValueEnumerable().FirstOrDefault(l => l.Language == language);
                if (langItem is null)
                {
                    row.Languages.Add(new LocalizationLanguageItem(language, filePath, value));
                }
                else
                {
                    langItem.Value = value;
                }
            }
        }

        return rowsMap.Values;
    }

    protected override (GameLanguage, FrozenDictionary<string, string>) ParseFileToContent(
        YAMLLocalisationParser.LocFile result
    )
    {
        var language = LanguageMap
            .AsValueEnumerable()
            .First(item => result.Key.EqualsIgnoreCase(item.Key))
            .Enum;
        var localisations = new Dictionary<string, string>(result.Entries.Count);
        foreach (var item in result.Entries)
        {
            localisations[item.Key] = item.Desc;
        }

        return (language, localisations.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
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

    protected override async Task<YAMLLocalisationParser.LocFile?> GetParseResultAsync(string filePath)
    {
        var localisation = YAMLLocalisationParser.ParseLocText(
            await File.ReadAllTextAsync(filePath, Encoding.UTF8).ConfigureAwait(false),
            filePath
        );
        if (localisation.IsFailure)
        {
            Log.LogParseError(localisation.GetError()!);
            return null;
        }

        var result = localisation.GetResult()!;
        return result;
    }
}
