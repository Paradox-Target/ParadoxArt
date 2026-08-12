using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.UnitTests.Services.GameResources;

[TestFixture]
public sealed class TerrainServiceTests
{
    private string _testRunDirectory = null!;
    private string _gameDirectory = null!;
    private string _modDirectory = null!;
    private ServiceProvider? _serviceProvider;

    [SetUp]
    public void SetUp()
    {
        _testRunDirectory = TestHelper.CreateUniqueTempDirectory();
        _gameDirectory = Path.Combine(_testRunDirectory, "game");
        _modDirectory = Path.Combine(_testRunDirectory, "mod");
        Directory.CreateDirectory(_gameDirectory);
        Directory.CreateDirectory(_modDirectory);
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

    [Test]
    public void OrderedLandTerrains_AppendsSpecialEnvironmentsAfterLandTerrains()
    {
        CreateGameTerrainFile(
            "00_terrain.txt",
            """
            categories = {
                forest = { }
                unknown = { }
                ocean = { is_water = yes }
            }
            """
        );
        var service = new TerrainService(BuildServiceProvider());

        Assert.That(
            service.LandTerrains,
            Is.EquivalentTo(["forest", "river", "snow", "fort", "river", "amphibious"])
        );
    }

    [Test]
    public void OrderedLandTerrains_PreservesFileOrderAfterReload()
    {
        string firstFilePath = CreateGameTerrainFile("00_terrain.txt", "categories = { forest = { } }");
        CreateGameTerrainFile("10_terrain.txt", "categories = { hills = { } }");
        var service = new TerrainService(BuildServiceProvider());

        ((IResourcesService)service).Reload(firstFilePath);

        Assert.That(
            service.LandTerrains,
            Is.EquivalentTo(["forest", "river", "snow", "hills", "fort", "amphibious"])
        );
    }

    private IServiceProvider BuildServiceProvider()
    {
        var settingsService = new SettingsService
        {
            GameRootFolderPath = _gameDirectory,
            ModRootFolderPath = _modDirectory
        };
        var descriptorService = new GameModDescriptorService(
            new SettingsService { ModRootFolderPath = Path.Combine(_testRunDirectory, "no_descriptor") }
        );
        var services = new ServiceCollection();
        services.AddSingleton(settingsService);
        services.AddSingleton(descriptorService);
        services.AddSingleton(new ProjectConfigService());
        services.AddSingleton<GameResourcesWatcherService>();
        services.AddSingleton<GameResourcesPathService>();
        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }

    private string CreateGameTerrainFile(string fileName, string content)
    {
        string directory = Path.Combine(_gameDirectory, "common", "terrain");
        Directory.CreateDirectory(directory);
        string filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
