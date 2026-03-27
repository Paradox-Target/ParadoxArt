using Hoi4BlueprintBuilder.Core.Services;

namespace Hoi4BlueprintBuilder.UnitTests.Services;

[TestFixture(TestOf = typeof(GameModDescriptorService))]
public class GameModDescriptorServiceTests
{
    private string _testDataPath;

    [SetUp]
    public void Setup()
    {
        _testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
    }

    [Test]
    public void Constructor_ShouldParseDescriptorFile_WhenFileExists()
    {
        // Arrange
        var settingsService = new SettingsService { ModRootFolderPath = _testDataPath };

        // Act
        var service = new GameModDescriptorService(settingsService);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(service.Name, Is.EqualTo("testName"));
            Assert.That(service.ReplacePaths, Has.Count.EqualTo(2));
            Assert.That(service.DependenciesName.Length, Is.EqualTo(1));
        }
        Assert.That(service.DependenciesName, Does.Contain("testDependency"));
        Assert.That(service.ReplacePaths, Contains.Item("events"));

        // Path.Combine will use backslash on Windows, so we expect "gfx\loadingscreens"
        string expectedGfxPath = Path.Combine("gfx", "loadingscreens");
        Assert.That(service.ReplacePaths, Contains.Item(expectedGfxPath));
    }

    [Test]
    public void Constructor_ShouldHandleMissingFile_Gracefully()
    {
        // Arrange
        var settingsService = new SettingsService
        {
            ModRootFolderPath = Path.Combine(_testDataPath, "NonExistent")
        };

        // Act
        var service = new GameModDescriptorService(settingsService);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.Name, Is.Empty);
            Assert.That(service.ReplacePaths, Is.Empty);
            Assert.That(service.DependenciesName, Is.Empty);
        }
    }
}
