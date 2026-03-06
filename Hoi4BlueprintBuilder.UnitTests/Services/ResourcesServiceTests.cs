using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.UnitTests.Services;

[TestFixture(TestOf = typeof(ResourcesService<,,>))]
public class ResourcesServiceTests
{
    private string _testRunDirectory = null!;
    private string _gameDir = null!;
    private string _modDir = null!;
    private ServiceProvider? _serviceProvider;

    [SetUp]
    public void Setup()
    {
        _testRunDirectory = TestHelper.CreateUniqueTempDirectory();
        _gameDir = Path.Combine(_testRunDirectory, "game");
        _modDir = Path.Combine(_testRunDirectory, "mod");
        Directory.CreateDirectory(_gameDir);
        Directory.CreateDirectory(_modDir);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
        if (Directory.Exists(_testRunDirectory))
        {
            Directory.Delete(_testRunDirectory, true);
        }
    }

    private IServiceProvider BuildServiceProvider()
    {
        var settingsService = new SettingsService
        {
            GameRootFolderPath = _gameDir,
            ModRootFolderPath = _modDir,
        };
        var descriptorService = new GameModDescriptorService(
            new SettingsService { ModRootFolderPath = Path.Combine(_testRunDirectory, "no_descriptor") }
        );
        var services = new ServiceCollection();
        services.AddSingleton(settingsService);
        services.AddSingleton(descriptorService);
        services.AddSingleton<GameResourcesWatcherService>();
        services.AddSingleton<GameResourcesPathService>();
        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }

    private void CreateGameFile(string relativePath, string content)
    {
        string fullPath = Path.Combine(_gameDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private void CreateModFile(string relativePath, string content)
    {
        string fullPath = Path.Combine(_modDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_FolderPath_ShouldLoadAllFiles()
    {
        CreateGameFile("testfolder/file1.txt", "content1");
        CreateGameFile("testfolder/file2.txt", "content2");
        var sp = BuildServiceProvider();

        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        Assert.That(service.ExposedResources, Has.Count.EqualTo(2));
    }

    [Test]
    public void Constructor_FolderPath_ShouldPreferModOverGame()
    {
        CreateGameFile("testfolder/file1.txt", "game_content");
        CreateModFile("testfolder/file1.txt", "mod_content");
        var sp = BuildServiceProvider();

        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources, Has.Count.EqualTo(1));
            Assert.That(service.ExposedResources.Values.First(), Is.EqualTo("mod_content"));
        }
    }

    [Test]
    public void Constructor_FolderPath_WhenGameFolderNotExist_ShouldThrow()
    {
        var sp = BuildServiceProvider();

        Assert.Throws<DirectoryNotFoundException>(
            () => TestResourcesService.CreateForFolder("nonexistent", sp)
        );
    }

    [Test]
    public void Constructor_FolderPath_EmptyFolder_ShouldHaveEmptyResources()
    {
        Directory.CreateDirectory(Path.Combine(_gameDir, "emptyfolder"));
        var sp = BuildServiceProvider();

        var service = TestResourcesService.CreateForFolder("emptyfolder", sp);

        Assert.That(service.ExposedResources, Is.Empty);
    }

    [Test]
    public void Constructor_FilePath_ShouldLoadSingleFile()
    {
        CreateGameFile("testfolder/file1.txt", "content1");
        var sp = BuildServiceProvider();

        var service = TestResourcesService.CreateForFile("testfolder/file1.txt", sp);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources, Has.Count.EqualTo(1));
            Assert.That(service.ExposedResources.Values.First(), Is.EqualTo("content1"));
        }
    }

    [Test]
    public void Constructor_FilePath_WhenFileNotFound_ShouldHaveEmptyResources()
    {
        var sp = BuildServiceProvider();

        var service = TestResourcesService.CreateForFile("testfolder/nonexistent.txt", sp);

        Assert.That(service.ExposedResources, Is.Empty);
    }

    [Test]
    public void Constructor_AsyncLoading_ShouldLoadAllFiles()
    {
        CreateGameFile("testfolder/file1.txt", "content1");
        CreateGameFile("testfolder/file2.txt", "content2");
        var sp = BuildServiceProvider();

        var service = TestResourcesService.CreateForFolder("testfolder", sp, isAsyncLoading: true);

        Assert.That(service.ExposedResources, Has.Count.EqualTo(2));
    }

    #endregion

    #region Add Tests

    [Test]
    public void Add_ShouldAddModResource_AndRemoveGameResource()
    {
        CreateGameFile("testfolder/file1.txt", "game_content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string gameFilePath = Path.Combine(_gameDir, "testfolder", "file1.txt");
        Assert.That(service.ExposedResources.ContainsKey(gameFilePath), Is.True);

        CreateModFile("testfolder/file1.txt", "mod_content");
        string modFilePath = Path.Combine(_modDir, "testfolder", "file1.txt");
        ((IResourcesService)service).Add(modFilePath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources.ContainsKey(gameFilePath), Is.False);
            Assert.That(service.ExposedResources[modFilePath], Is.EqualTo("mod_content"));
        }
    }

    [Test]
    public void Add_ShouldFireOnResourceChanged()
    {
        CreateGameFile("testfolder/file1.txt", "game_content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        ResourceChangedEventArgs? receivedArgs = null;
        service.OnResourceChanged += (_, args) => receivedArgs = args;

        CreateModFile("testfolder/file1.txt", "mod_content");
        string modFilePath = Path.Combine(_modDir, "testfolder", "file1.txt");
        ((IResourcesService)service).Add(modFilePath);

        Assert.That(receivedArgs, Is.Not.Null);
        Assert.That(receivedArgs!.FilePath, Is.EqualTo(modFilePath));
    }

    [Test]
    public void Add_WhenNoExistingGameResource_ShouldAddModResource()
    {
        CreateGameFile("testfolder/other.txt", "other_content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        CreateModFile("testfolder/newfile.txt", "new_content");
        string modFilePath = Path.Combine(_modDir, "testfolder", "newfile.txt");
        ((IResourcesService)service).Add(modFilePath);

        Assert.That(service.ExposedResources, Has.Count.EqualTo(2));
        Assert.That(service.ExposedResources[modFilePath], Is.EqualTo("new_content"));
    }

    #endregion

    #region Remove Tests

    [Test]
    public void Remove_ShouldRemoveModResource_AndRestoreGameResource()
    {
        CreateGameFile("testfolder/file1.txt", "game_content");
        CreateModFile("testfolder/file1.txt", "mod_content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string modFilePath = Path.Combine(_modDir, "testfolder", "file1.txt");
        Assert.That(service.ExposedResources.ContainsKey(modFilePath), Is.True);

        ((IResourcesService)service).Remove(modFilePath);

        string gameFilePath = Path.Combine(_gameDir, "testfolder", "file1.txt");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources.ContainsKey(modFilePath), Is.False);
            Assert.That(service.ExposedResources.ContainsKey(gameFilePath), Is.True);
            Assert.That(service.ExposedResources[gameFilePath], Is.EqualTo("game_content"));
        }
    }

    [Test]
    public void Remove_WhenNoGameFallback_ShouldJustRemove()
    {
        CreateGameFile("testfolder/placeholder.txt", "x");
        CreateModFile("testfolder/modonly.txt", "mod_only_content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string modFilePath = Path.Combine(_modDir, "testfolder", "modonly.txt");
        Assert.That(service.ExposedResources.ContainsKey(modFilePath), Is.True);

        ((IResourcesService)service).Remove(modFilePath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources.ContainsKey(modFilePath), Is.False);
            Assert.That(service.ExposedResources, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Remove_ShouldFireOnResourceChanged_WhenGameFallbackExists()
    {
        CreateGameFile("testfolder/file1.txt", "game_content");
        CreateModFile("testfolder/file1.txt", "mod_content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        ResourceChangedEventArgs? receivedArgs = null;
        service.OnResourceChanged += (_, args) => receivedArgs = args;

        string modFilePath = Path.Combine(_modDir, "testfolder", "file1.txt");
        ((IResourcesService)service).Remove(modFilePath);

        Assert.That(receivedArgs, Is.Not.Null);
        Assert.That(receivedArgs!.FilePath, Is.EqualTo(modFilePath));
    }

    [Test]
    public void Remove_WhenResourceDoesNotExist_ShouldNotFireEvent()
    {
        CreateGameFile("testfolder/file1.txt", "game_content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        ResourceChangedEventArgs? receivedArgs = null;
        service.OnResourceChanged += (_, args) => receivedArgs = args;

        ((IResourcesService)service).Remove(Path.Combine(_modDir, "testfolder", "nonexistent.txt"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receivedArgs, Is.Null);
            Assert.That(service.ExposedResources, Has.Count.EqualTo(1));
        }
    }

    #endregion

    #region Reload Tests

    [Test]
    public void Reload_ShouldReloadFileContent()
    {
        CreateGameFile("testfolder/file1.txt", "original_content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string filePath = Path.Combine(_gameDir, "testfolder", "file1.txt");
        Assert.That(service.ExposedResources[filePath], Is.EqualTo("original_content"));

        File.WriteAllText(filePath, "updated_content");
        ((IResourcesService)service).Reload(filePath);

        Assert.That(service.ExposedResources[filePath], Is.EqualTo("updated_content"));
    }

    [Test]
    public void Reload_ShouldFireOnResourceChanged()
    {
        CreateGameFile("testfolder/file1.txt", "content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        ResourceChangedEventArgs? receivedArgs = null;
        service.OnResourceChanged += (_, args) => receivedArgs = args;

        string filePath = Path.Combine(_gameDir, "testfolder", "file1.txt");
        ((IResourcesService)service).Reload(filePath);

        Assert.That(receivedArgs, Is.Not.Null);
        Assert.That(receivedArgs!.FilePath, Is.EqualTo(filePath));
    }

    [Test]
    public void Reload_WhenDirectory_ShouldSkip()
    {
        CreateGameFile("testfolder/file1.txt", "content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string folderPath = Path.Combine(_gameDir, "testfolder");
        ((IResourcesService)service).Reload(folderPath);

        Assert.That(service.ExposedResources, Has.Count.EqualTo(1));
    }

    [Test]
    public void Reload_WhenParseResultIsNull_ShouldNotFireEvent()
    {
        CreateGameFile("testfolder/file1.txt", "content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        ResourceChangedEventArgs? receivedArgs = null;
        service.OnResourceChanged += (_, args) => receivedArgs = args;

        // 不存在的文件, GetParseResult 返回 null
        string filePath = Path.Combine(_gameDir, "testfolder", "nonexistent.txt");
        ((IResourcesService)service).Reload(filePath);

        Assert.That(receivedArgs, Is.Null);
    }

    [Test]
    public void Reload_WhenFileDeleted_ShouldRemoveResourceWithoutEvent()
    {
        CreateGameFile("testfolder/file1.txt", "content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string filePath = Path.Combine(_gameDir, "testfolder", "file1.txt");
        Assert.That(service.ExposedResources, Has.Count.EqualTo(1));

        File.Delete(filePath);

        ResourceChangedEventArgs? receivedArgs = null;
        service.OnResourceChanged += (_, args) => receivedArgs = args;
        ((IResourcesService)service).Reload(filePath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources, Is.Empty);
            Assert.That(receivedArgs, Is.Null);
        }
    }

    #endregion

    #region Renamed Tests

    [Test]
    public void Renamed_ShouldUpdateResourceKey()
    {
        CreateGameFile("testfolder/file1.txt", "content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string oldPath = Path.Combine(_gameDir, "testfolder", "file1.txt");
        string newPath = Path.Combine(_gameDir, "testfolder", "file_renamed.txt");
        Assert.That(service.ExposedResources.ContainsKey(oldPath), Is.True);

        ((IResourcesService)service).Renamed(oldPath, newPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources.ContainsKey(oldPath), Is.False);
            Assert.That(service.ExposedResources.ContainsKey(newPath), Is.True);
            Assert.That(service.ExposedResources[newPath], Is.EqualTo("content"));
        }
    }

    [Test]
    public void Renamed_WhenOldPathDoesNotExist_ShouldDoNothing()
    {
        CreateGameFile("testfolder/file1.txt", "content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string oldPath = Path.Combine(_gameDir, "testfolder", "nonexistent.txt");
        string newPath = Path.Combine(_gameDir, "testfolder", "new.txt");

        ((IResourcesService)service).Renamed(oldPath, newPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources, Has.Count.EqualTo(1));
            Assert.That(service.ExposedResources.ContainsKey(newPath), Is.False);
        }
    }

    [Test]
    public void Renamed_WhenNewPathIsDirectory_ShouldSkip()
    {
        CreateGameFile("testfolder/file1.txt", "content");
        var sp = BuildServiceProvider();
        var service = TestResourcesService.CreateForFolder("testfolder", sp);

        string oldPath = Path.Combine(_gameDir, "testfolder", "file1.txt");
        string newPath = Path.Combine(_gameDir, "testfolder"); // 这是一个文件夹

        ((IResourcesService)service).Renamed(oldPath, newPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.ExposedResources, Has.Count.EqualTo(1));
            Assert.That(service.ExposedResources.ContainsKey(oldPath), Is.True);
        }
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestResourcesService : ResourcesService<TestResourcesService, string, string>
    {
        public IDictionary<string, string> ExposedResources => Resources;

        private TestResourcesService(
            string folderOrFileRelativePath,
            IServiceProvider serviceProvider,
            PathType pathType,
            SearchOption searchOption = SearchOption.TopDirectoryOnly,
            bool isAsyncLoading = false
        )
            : base(
                folderOrFileRelativePath,
                WatcherFilter.Text,
                serviceProvider,
                pathType,
                searchOption,
                isAsyncLoading
            ) { }

        public static TestResourcesService CreateForFolder(
            string folderRelativePath,
            IServiceProvider serviceProvider,
            SearchOption searchOption = SearchOption.TopDirectoryOnly,
            bool isAsyncLoading = false
        )
        {
            return new TestResourcesService(
                folderRelativePath,
                serviceProvider,
                PathType.Folder,
                searchOption,
                isAsyncLoading
            );
        }

        public static TestResourcesService CreateForFile(
            string fileRelativePath,
            IServiceProvider serviceProvider
        )
        {
            return new TestResourcesService(fileRelativePath, serviceProvider, PathType.File);
        }

        protected override string? GetParseResult(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            return File.ReadAllText(filePath);
        }

        protected override Task<string?> GetParseResultAsync(string filePath)
        {
            return Task.FromResult(GetParseResult(filePath));
        }

        protected override string? ParseFileToContent(string result) => result;
    }

    #endregion
}
