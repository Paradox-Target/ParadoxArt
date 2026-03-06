using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;

namespace Hoi4BlueprintBuilder.UnitTests.Services;

[TestFixture(TestOf = typeof(GameResourcesWatcherService))]
public class GameResourcesWatcherServiceTests
{
    private string _testRunDirectory = null!;
    private string _modDir = null!;
    private GameResourcesWatcherService _watcherService = null!;

    [SetUp]
    public void Setup()
    {
        _testRunDirectory = TestHelper.CreateUniqueTempDirectory();
        _modDir = Path.Combine(_testRunDirectory, "mod");
        Directory.CreateDirectory(_modDir);

        var settingsService = new SettingsService { ModRootFolderPath = _modDir };
        _watcherService = new GameResourcesWatcherService(settingsService);
    }

    [TearDown]
    public void TearDown()
    {
        _watcherService.Dispose();
        if (Directory.Exists(_testRunDirectory))
        {
            Directory.Delete(_testRunDirectory, true);
        }
    }

    #region Watch Tests

    [Test]
    public void Watch_ExistingFolder_ShouldNotThrow()
    {
        string folder = "common";
        Directory.CreateDirectory(Path.Combine(_modDir, folder));
        var mock = new MockResourcesService();

        Assert.DoesNotThrow(() => _watcherService.Watch(folder, mock));
    }

    [Test]
    public void Watch_NonExistingFolder_ShouldNotThrow()
    {
        var mock = new MockResourcesService();

        Assert.DoesNotThrow(() => _watcherService.Watch("non_existing", mock));
    }

    [Test]
    public void Watch_SameFolderTwice_ShouldNotThrow()
    {
        string folder = "common";
        Directory.CreateDirectory(Path.Combine(_modDir, folder));
        var mock1 = new MockResourcesService();
        var mock2 = new MockResourcesService();

        _watcherService.Watch(folder, mock1);

        Assert.DoesNotThrow(() => _watcherService.Watch(folder, mock2));
    }

    #endregion

    #region Unwatch Tests

    [Test]
    public void Unwatch_WatchedFolder_ShouldNotThrow()
    {
        string folder = "common";
        Directory.CreateDirectory(Path.Combine(_modDir, folder));
        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        Assert.DoesNotThrow(() => _watcherService.Unwatch(folder));
    }

    [Test]
    public void Unwatch_UnwatchedFolder_ShouldNotThrow()
    {
        Assert.DoesNotThrow(() => _watcherService.Unwatch("never_watched"));
    }

    [Test]
    public void Unwatch_WaitingFolder_ShouldRemoveFromWaitingList()
    {
        string folder = "non_existing";
        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        // Unwatch should remove from waiting list without error
        Assert.DoesNotThrow(() => _watcherService.Unwatch(folder));
    }

    [Test]
    public void Unwatch_AfterWatch_SameFolderTwice_ShouldNotThrow()
    {
        string folder = "common";
        Directory.CreateDirectory(Path.Combine(_modDir, folder));
        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        _watcherService.Unwatch(folder);
        // Second unwatch should be a no-op
        Assert.DoesNotThrow(() => _watcherService.Unwatch(folder));
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_ShouldNotThrow()
    {
        string folder = "common";
        Directory.CreateDirectory(Path.Combine(_modDir, folder));
        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        Assert.DoesNotThrow(() => _watcherService.Dispose());
    }

    [Test]
    public void Dispose_Twice_ShouldNotThrow()
    {
        _watcherService.Dispose();

        Assert.DoesNotThrow(() => _watcherService.Dispose());
    }

    [Test]
    public void Dispose_WithWaitingFolder_ShouldNotThrow()
    {
        var mock = new MockResourcesService();
        _watcherService.Watch("non_existing", mock);

        Assert.DoesNotThrow(() => _watcherService.Dispose());
    }

    #endregion

    #region File Event Integration Tests

    [Test]
    public void FileCreated_ShouldCallAdd()
    {
        string folder = "events";
        string folderPath = Path.Combine(_modDir, folder);
        Directory.CreateDirectory(folderPath);

        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        string newFile = Path.Combine(folderPath, "test_event.txt");
        File.WriteAllText(newFile, "content");

        Assert.That(() => mock.AddedPaths, Has.Count.GreaterThan(0).After(3000, 100));
        Assert.That(mock.AddedPaths, Does.Contain(newFile));
    }

    [Test]
    public void FileDeleted_ShouldCallRemove()
    {
        string folder = "events";
        string folderPath = Path.Combine(_modDir, folder);
        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, "to_delete.txt");
        File.WriteAllText(filePath, "content");

        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        File.Delete(filePath);

        Assert.That(() => mock.RemovedPaths, Has.Count.GreaterThan(0).After(3000, 100));
        Assert.That(mock.RemovedPaths, Does.Contain(filePath));
    }

    [Test]
    public void FileChanged_ShouldCallReload()
    {
        string folder = "events";
        string folderPath = Path.Combine(_modDir, folder);
        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, "to_change.txt");
        File.WriteAllText(filePath, "original");

        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        File.WriteAllText(filePath, "modified");

        Assert.That(() => mock.ReloadedPaths, Has.Count.GreaterThan(0).After(3000, 100));
        Assert.That(mock.ReloadedPaths, Does.Contain(filePath));
    }

    [Test]
    public void FileRenamed_ShouldCallRenamed()
    {
        string folder = "events";
        string folderPath = Path.Combine(_modDir, folder);
        Directory.CreateDirectory(folderPath);

        string oldPath = Path.Combine(folderPath, "old_name.txt");
        string newPath = Path.Combine(folderPath, "new_name.txt");
        File.WriteAllText(oldPath, "content");

        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        File.Move(oldPath, newPath);

        Assert.That(() => mock.RenamedPaths, Has.Count.GreaterThan(0).After(3000, 100));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(mock.RenamedPaths[0].OldPath, Is.EqualTo(oldPath));
            Assert.That(mock.RenamedPaths[0].NewPath, Is.EqualTo(newPath));
        }
    }

    #endregion

    #region Waiting Folder Tests

    [Test]
    public void Watch_NonExistingFolder_ThenCreate_ShouldStartWatching()
    {
        string folder = "new_folder";
        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        // Create the folder - mod folder watcher should detect it
        string folderPath = Path.Combine(_modDir, folder);
        // Now create a file in the newly watched folder
        string newFile = Path.Combine(folderPath, "test.txt");

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(newFile, "content");

        // The folder was new, so existing files (including test.txt written before watcher starts)
        // should have been added via the isNewFolder path
        Assert.That(() => mock.AddedPaths, Has.Count.EqualTo(1).After(3000, 100));
        Assert.That(mock.AddedPaths, Does.Contain(newFile));
    }

    [Test]
    public void Watch_NonExistingFolder_ThenCreate_WithExistingFiles_ShouldAddExistingFiles()
    {
        string folder = "created_folder";
        var mock = new MockResourcesService();
        _watcherService.Watch(folder, mock);

        // Create the folder with files
        string folderPath = Path.Combine(_modDir, folder);
        string filePath1 = Path.Combine(folderPath, "file1.txt");
        string filePath2 = Path.Combine(folderPath, "file2.txt");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(filePath1, "content1");
        File.WriteAllText(filePath2, "content2");

        // Wait for the mod watcher to detect and enumerate
        Assert.That(() => mock.AddedPaths, Has.Count.EqualTo(2).After(5000, 100));
        Assert.That(mock.AddedPaths, Is.EquivalentTo([filePath1, filePath2]));
    }

    #endregion

    #region Mock

    private sealed class MockResourcesService : IResourcesService
    {
        private readonly Lock _lock = new();

        public List<string> AddedPaths { get; } = [];
        public List<string> RemovedPaths { get; } = [];
        public List<string> ReloadedPaths { get; } = [];
        public List<(string OldPath, string NewPath)> RenamedPaths { get; } = [];

        public void Add(string folderOrFilePath)
        {
            lock (_lock)
            {
                AddedPaths.Add(folderOrFilePath);
            }
        }

        public void Remove(string folderOrFilePath)
        {
            lock (_lock)
            {
                RemovedPaths.Add(folderOrFilePath);
            }
        }

        public void Reload(string folderOrFilePath)
        {
            lock (_lock)
            {
                ReloadedPaths.Add(folderOrFilePath);
            }
        }

        public void Renamed(string oldPath, string newPath)
        {
            lock (_lock)
            {
                RenamedPaths.Add((oldPath, newPath));
            }
        }
    }

    #endregion
}
