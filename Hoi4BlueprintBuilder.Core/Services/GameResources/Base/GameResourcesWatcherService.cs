using Hoi4BlueprintBuilder.Core.Infrastructure;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources.Base;

/// <summary>
/// 用来监听游戏资源的改变, 如注册的国家标签, 资源, 建筑物
/// </summary>
[RegisterSingleton<GameResourcesWatcherService>]
public sealed class GameResourcesWatcherService : IDisposable
{
    //TODO: 性能优化: 只使用一个 FileSystemSafeWatcher 来监听 Mod文件夹, 当有事件发生时, 根据事件路径判断是哪个资源文件夹, 从而通知对应的资源服务
    /// <summary>
    /// key: 资源文件夹路径, value: 监听器列表
    /// </summary>
    private readonly Dictionary<string, List<FileSystemSafeWatcher>> _watchedPaths = new(8);
    private readonly FileSystemSafeWatcher _modFolderWatcher;

    //TODO: 重构, 可以用这个类监测资源服务的变化并发出通知
    //监听者???, 消息总线?

    /// <summary>
    /// 待监听文件夹列表, 其中的文件夹被创建或从其他名称重命名后, 会被自动监听, 然后被移除
    /// </summary>
    private readonly List<(
        string folderRelativePath,
        IResourcesService resourcesService,
        string filter,
        bool includeSubFolders
    )> _waitingWatchFolders = new(8);
    private readonly Lock _lock = new();
    private readonly SettingsService _settingService;
    private bool _disposed;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public GameResourcesWatcherService(SettingsService settingService)
    {
        _settingService = settingService;
        _modFolderWatcher = new FileSystemSafeWatcher(_settingService.ModRootFolderPath, "*.*");
        _modFolderWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
        _modFolderWatcher.Created += OnModResourceFolderCreatedOrRenamed;
        _modFolderWatcher.Renamed += OnModResourceFolderCreatedOrRenamed;
        _modFolderWatcher.IncludeSubdirectories = true;
        _modFolderWatcher.EnableRaisingEvents = true;
    }

    private void OnModResourceFolderCreatedOrRenamed(object sender, FileSystemEventArgs args)
    {
        string relativePath = Path.GetRelativePath(_settingService.ModRootFolderPath, args.FullPath);
        lock (_lock)
        {
            int index = _waitingWatchFolders.FindIndex(tuple => tuple.folderRelativePath == relativePath);
            if (index != -1)
            {
                (_, var resourcesService, string filter, bool includeSubFolders) = _waitingWatchFolders[
                    index
                ];
                _waitingWatchFolders.RemoveAt(index);
                WatchInternal(relativePath, resourcesService, filter, includeSubFolders, true);

                Log.Info("等待监听的文件夹 '{FolderName}' 被创建或重命名, 从待监听列表中移除并开始监听", Path.GetFileName(args.FullPath));
            }
        }
    }

    public void Watch(
        string folderRelativePath,
        IResourcesService resourcesService,
        string filter = "*.*",
        bool includeSubFolders = false
    )
    {
        lock (_lock)
        {
            WatchInternal(folderRelativePath, resourcesService, filter, includeSubFolders, false);
        }
    }

    /// <remarks>调用方必须持有 <see cref="_lock"/></remarks>
    private void WatchInternal(
        string folderRelativePath,
        IResourcesService resourcesService,
        string filter,
        bool includeSubFolders,
        bool isNewFolder
    )
    {
        string modFolderPath = Path.Combine(_settingService.ModRootFolderPath, folderRelativePath);
        // 如果 Mod文件夹 不存在, 监听上一级文件夹, 当 Mod文件夹 创建后, 自动监听
        if (!Directory.Exists(modFolderPath))
        {
            _waitingWatchFolders.Add((folderRelativePath, resourcesService, filter, includeSubFolders));

            Log.Info("Mod 目录中 '{FolderPath}' 文件夹不存在, 无法监听, 已添加到等待监听列表", folderRelativePath);
            Log.Debug("Path: {FolderPath}", modFolderPath);
            return;
        }

        var watcher = new FileSystemSafeWatcher(modFolderPath, filter);
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        watcher.Changed += (_, args) =>
        {
            resourcesService.Reload(args.FullPath);

            Log.Debug("资源文件: {Path} 发生变化, 类型: {ChangeType}", args.FullPath, args.ChangeType);
        };
        watcher.Renamed += (_, args) => resourcesService.Renamed(args.OldFullPath, args.FullPath);
        watcher.Created += (_, args) => resourcesService.Add(args.FullPath);
        watcher.Deleted += (_, args) => resourcesService.Remove(args.FullPath);
        watcher.IncludeSubdirectories = includeSubFolders;
        watcher.EnableRaisingEvents = true;

        if (_watchedPaths.TryGetValue(folderRelativePath, out var watcherList))
        {
            Log.Debug("多个服务监听同一文件夹, Path: {FolderPath}", modFolderPath);
            watcherList.Add(watcher);
        }
        else
        {
            _watchedPaths.Add(folderRelativePath, [watcher]);
        }

        // 当创建文件夹和创建文件在很短的时间内先后发生时, 可能会导致来不及监听新文件夹, 错过创建文件的事件
        // 因此在监听新文件夹时, 需要额外枚举一次现有的文件
        if (isNewFolder)
        {
            var searchOption = includeSubFolders
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            foreach (string enumerateFile in Directory.EnumerateFiles(modFolderPath, filter, searchOption))
            {
                resourcesService.Add(enumerateFile);
            }
        }

        Log.Info("开始监听资源文件夹: {FolderPath}", modFolderPath);
    }

    public void Unwatch(string folderRelativePath)
    {
        Log.Debug("尝试停止监听资源文件夹: {FolderPath}", folderRelativePath);
        lock (_lock)
        {
            if (_watchedPaths.TryGetValue(folderRelativePath, out var watcherList))
            {
                watcherList.ForEach(static watcher => watcher.Dispose());
                _watchedPaths.Remove(folderRelativePath);
                Log.Info("成功停止监听资源文件夹: {FolderPath}", folderRelativePath);
            }

            bool isRemoved =
                _waitingWatchFolders.RemoveAll(tuple => tuple.folderRelativePath == folderRelativePath) != 0;
            if (isRemoved)
            {
                Log.Info("从待监听文件夹列表中移除: {FolderPath}", folderRelativePath);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _modFolderWatcher.Dispose();
        lock (_lock)
        {
            foreach (var watcher in _watchedPaths.Values.SelectMany(static watcherList => watcherList))
            {
                watcher.Dispose();
            }
            _watchedPaths.Clear();
            _waitingWatchFolders.Clear();
        }
    }
}
