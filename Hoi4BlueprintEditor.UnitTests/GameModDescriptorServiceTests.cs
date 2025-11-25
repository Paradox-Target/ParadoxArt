using Hoi4BlueprintEditor.Services;

namespace Hoi4BlueprintEditor.UnitTests;

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

        // Assert
        Assert.That(service.Name, Is.EqualTo("testName"));
        Assert.That(service.ReplacePaths, Has.Count.EqualTo(2));
        Assert.That(service.ReplacePaths, Contains.Item("events"));

        // Path.Combine will use backslash on Windows, so we expect "gfx\loadingscreens"
        var expectedGfxPath = Path.Combine("gfx", "loadingscreens");
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
        }
    }
}
