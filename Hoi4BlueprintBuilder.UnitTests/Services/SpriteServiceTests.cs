using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.UnitTests.Services;

[TestFixture(TestOf = typeof(SpriteService))]
public class SpriteServiceTests
{
    private string _testRunDirectory;
    private SpriteService _spriteService;
    private ServiceProvider _serviceProvider;

    private readonly string _sourceTestDataPath1 = Path.Combine(
        TestApp.TestDataDirectory,
        "interface",
        "test1.gfx"
    );

    private readonly string _sourceTestDataPath2 = Path.Combine(
        TestApp.TestDataDirectory,
        "interface",
        "test2.gfx"
    );

    [SetUp]
    public void Setup()
    {
        _testRunDirectory = TestHelper.CreateUniqueTempDirectory();
        TestContext.Out.WriteLine($"测试临时文件夹: {_testRunDirectory}");

        var interfaceDir = Path.Combine(_testRunDirectory, "interface");
        Directory.CreateDirectory(interfaceDir);

        File.Copy(_sourceTestDataPath1, Path.Combine(interfaceDir, "test1.gfx"));
        File.Copy(_sourceTestDataPath2, Path.Combine(interfaceDir, "test2.gfx"));

        var settingsService = new SettingsService
        {
            ModRootFolderPath = _testRunDirectory,
            GameRootFolderPath = _testRunDirectory,
            GameLanguage = GameLanguage.Chinese
        };
        var services = new ServiceCollection();
        var descriptorService = new GameModDescriptorService(
            new SettingsService { ModRootFolderPath = TestApp.TestDataDirectory }
        );

        services.AddSingleton(settingsService);
        services.AddSingleton(descriptorService);
        services.AddSingleton<GameResourcesWatcherService>();
        services.AddSingleton<GameResourcesPathService>();
        services.AddSingleton<SpriteService>();
        _serviceProvider = services.BuildServiceProvider();
        _spriteService = _serviceProvider.GetRequiredService<SpriteService>();
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
    public void TryGetSpriteInfo_ShouldLoadSpritesFromTestData()
    {
        // Test 1: Basic sprite from test1.gfx
        bool result1 = _spriteService.TryGetSpriteInfo("GFX_test_1", out var info1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result1, Is.True);
            Assert.That(info1, Is.Not.Null);
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(info1.Name, Is.EqualTo("GFX_test_1"));
            Assert.That(info1.RelativePath, Is.EqualTo("gfx/GFX_test_1.dds"));
            Assert.That(info1.TotalFrames, Is.EqualTo(1));
        }

        bool result2 = _spriteService.TryGetSpriteInfo("GFX_test_2", out var info2);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result2, Is.True);
            Assert.That(info2, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info2.Name, Is.EqualTo("GFX_test_2"));
            Assert.That(info2.RelativePath, Is.EqualTo("gfx/GFX_test_2.dds"));
            Assert.That(info2.TotalFrames, Is.EqualTo(1));
        }

        // Test 2: Sprite from test2.gfx
        bool result3 = _spriteService.TryGetSpriteInfo("GFX_test_3", out var info3);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result3, Is.True);
            Assert.That(info3, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info3.Name, Is.EqualTo("GFX_test_3"));
            Assert.That(info3.RelativePath, Is.EqualTo("gfx/GFX_test_3.dds"));
            Assert.That(info3.TotalFrames, Is.EqualTo(1));
        }

        // Test 3: Sprite with frames from test2.gfx
        var result4 = _spriteService.TryGetSpriteInfo("GFX_test_4", out var info4);
        Assert.That(result4, Is.True);
        Assert.That(info4, Is.Not.Null);
        Assert.That(info4.Name, Is.EqualTo("GFX_test_4"));
        Assert.That(info4.RelativePath, Is.EqualTo("gfx/GFX_test_4.dds"));
        Assert.That(info4.TotalFrames, Is.EqualTo(7));
    }

    [Test]
    public void TryGetSpriteInfo_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        bool result = _spriteService.TryGetSpriteInfo("NON_EXISTENT_SPRITE", out var info);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(info, Is.Null);
        }
    }
}
