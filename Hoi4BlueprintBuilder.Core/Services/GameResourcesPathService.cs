using Hoi4BlueprintBuilder.Core.Helpers;
using NLog;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<GameResourcesPathService>]
public sealed class GameResourcesPathService(
    SettingsService settingService,
    GameModDescriptorService descriptor,
    ProjectConfigService projectConfigService
)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 获得所有应该加载的文件绝对路径, Mod优先, 遵循 replace_path 指令
    /// </summary>
    /// <param name="folderRelativePath"></param>
    /// <param name="filter"></param>
    /// <param name="searchOption"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public IReadOnlyCollection<string> GetAllFilePriorModByRelativePathForFolder(
        string folderRelativePath,
        string filter,
        SearchOption searchOption
    )
    {
        Log.Info("正在获取文件夹 {Path} 下的文件", folderRelativePath);
        using var dependenciesPath = projectConfigService
            .Dependencies.AsValueEnumerable()
            .Select(info => Path.Combine(info.RootDirectory, folderRelativePath))
            .ToArrayPool();
        string modFolder = Path.Combine(settingService.ModRootFolderPath, folderRelativePath);
        string gameFolder = Path.Combine(settingService.GameRootFolderPath, folderRelativePath);

        if (!Directory.Exists(gameFolder))
        {
            throw new DirectoryNotFoundException($"找不到文件夹 {gameFolder}");
        }

        if (!ExistsInAnyMod(modFolder, dependenciesPath.Span))
        {
            return Directory.GetFiles(gameFolder, filter, searchOption);
        }

        var modsFiles = GetModFiles(modFolder, dependenciesPath, filter, searchOption);
        if (descriptor.ReplacePaths.Contains(folderRelativePath))
        {
            Log.Debug("MOD文件夹已完全替换游戏文件夹: \n\t {GamePath} => {ModPath}", gameFolder, modFolder);
            return GetUniqueFiles([], modsFiles);
        }

        string[] gameFilesPath = Directory.GetFiles(gameFolder, filter, searchOption);
        return GetUniqueFiles(gameFilesPath, modsFiles);
    }

    private static IEnumerable<IEnumerable<string>> GetModFiles(
        string modFolder,
        PooledArray<string> dependenciesPath,
        string filter,
        SearchOption searchOption
    )
    {
        if (Directory.Exists(modFolder))
        {
            yield return Directory.EnumerateFiles(modFolder, filter, searchOption);
        }

        for (int index = 0; index < dependenciesPath.Size; index++)
        {
            string path = dependenciesPath.Array[index];
            if (Directory.Exists(path))
            {
                yield return Directory.EnumerateFiles(path, filter, searchOption);
            }
        }
    }

    /// <summary>
    /// 判断路径在游戏本体或者任一依赖模组中是否存在, 优先检查 Mod 路径
    /// </summary>
    /// <returns>若路径在任一模组中存在, 则返回<c>true</c></returns>
    private static bool ExistsInAnyMod(string modPath, Span<string> dependenciesPath)
    {
        if (Directory.Exists(modPath))
        {
            return true;
        }

        foreach (string path in dependenciesPath)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 移除重名文件, 优先移除游戏本体文件
    /// </summary>
    /// <param name="gameFilePaths"></param>
    /// <param name="modFilePaths"></param>
    /// <returns>不重名的文件路径</returns>
    /// <exception cref="ArgumentException"></exception>
    private static IReadOnlyCollection<string> GetUniqueFiles(
        string[] gameFilePaths,
        IEnumerable<IEnumerable<string>> modFilePaths
    )
    {
        var set = new Dictionary<string, string>(gameFilePaths.Length, PlatformHelper.Comparer);

        // 优先读取Mod文件
        // TODO: 做一下性能测试, 看和原来的算法有什么区别
        foreach (
            string filePath in modFilePaths
                .AsValueEnumerable()
                .SelectMany(files => files)
                .Concat(gameFilePaths)
        )
        {
            string fileName = Path.GetFileName(filePath);
            set.TryAdd(fileName, filePath);
        }

        return set.Values;
    }

    /// <summary>
    /// 根据相对路径获得游戏或者Mod文件的绝对路径, 优先Mod
    /// </summary>
    /// <remarks>
    /// 注意: 此方法会忽略mod描述文件中的 <c>replace_path</c> 指令
    /// </remarks>
    /// <param name="fileRelativePath">根目录下的相对路径</param>
    /// <returns>文件绝对路径, 未找到时返回<c>null</c></returns>
    public string? GetFilePathPriorModByRelativePath(string fileRelativePath)
    {
        string modFilePath = Path.Combine(settingService.ModRootFolderPath, fileRelativePath);
        modFilePath = Path.GetFullPath(modFilePath);
        if (File.Exists(modFilePath))
        {
            return modFilePath;
        }

        foreach (var dependencyModInfo in projectConfigService.Dependencies)
        {
            string path = Path.Combine(dependencyModInfo.RootDirectory, fileRelativePath);
            path = Path.GetFullPath(path);
            if (File.Exists(path))
            {
                return path;
            }
        }

        string gameFilePath = Path.Combine(settingService.GameRootFolderPath, fileRelativePath);
        gameFilePath = Path.GetFullPath(gameFilePath);
        if (File.Exists(gameFilePath))
        {
            return gameFilePath;
        }

        return null;
    }

    public FileOrigin GetFileOrigin(string filePath)
    {
        if (filePath.Contains(settingService.ModRootFolderPath))
        {
            return FileOrigin.Mod;
        }

        if (filePath.Contains(settingService.GameRootFolderPath))
        {
            return FileOrigin.Game;
        }
        return FileOrigin.Unknown;
    }
}
