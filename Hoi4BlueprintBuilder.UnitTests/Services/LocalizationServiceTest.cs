using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.UnitTests.Services;

[TestFixture(TestOf = typeof(LocalizationService))]
public class LocalizationServiceTests
{
    private string _testRunDirectory;
    private LocalizationService _localizationService;
    private ServiceProvider _serviceProvider;

    // 源测试数据路径 (相对于项目输出目录)
    private readonly string _sourceTestDataPath = Path.Combine(
        TestApp.TestDataDirectory,
        "localisation",
        "simp_chinese",
        "test.yml"
    );

    [SetUp]
    public void Setup()
    {
        // 1. 为每个测试创建一个唯一的临时目录，确保隔离
        _testRunDirectory = TestHelper.CreateUniqueTempDirectory();
        TestContext.Out.WriteLine($"测试临时文件夹: {_testRunDirectory}");

        // 2. 准备测试环境：模拟 Mod 目录结构
        // 我们使用简体中文环境，对应 test.yml
        var locDir = Path.Combine(_testRunDirectory, "localisation", "simp_chinese");
        Directory.CreateDirectory(locDir);

        // 3. 复制测试数据文件到临时目录
        File.Copy(_sourceTestDataPath, Path.Combine(locDir, "test_l_simp_chinese.yml"));

        // 4. 初始化 SettingsService，指向临时目录
        var settingsService = new SettingsService
        {
            ModRootFolderPath = _testRunDirectory,
            GameRootFolderPath = _testRunDirectory,
            GameLanguage = GameLanguage.Chinese
        };
        var services = new ServiceCollection();
        var descriptorService = new GameModDescriptorService(
            // 这里使用测试目录而不是临时目录, 因为不需要
            new SettingsService { ModRootFolderPath = TestApp.TestDataDirectory }
        );

        services.AddSingleton(settingsService);
        services.AddSingleton(descriptorService);
        services.AddSingleton<GameResourcesWatcherService>();
        services.AddSingleton<GameResourcesPathService>();
        services.AddMessagePipe();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton(new ProjectConfigService { SupportedLanguages = [GameLanguage.Chinese] });
        _serviceProvider = services.BuildServiceProvider();
        _localizationService = _serviceProvider.GetRequiredService<LocalizationService>();
    }

    [TearDown]
    public void TearDown()
    {
        // 清理资源
        _localizationService.Dispose();
        _serviceProvider?.Dispose();

        // 清理临时文件
        if (Directory.Exists(_testRunDirectory))
        {
            Directory.Delete(_testRunDirectory, true);
        }
    }

    [Test]
    public void GetValue_ShouldLoadValuesFromTestData()
    {
        // 验证是否成功加载了 test.yml 中的数据
        var value1 = _localizationService.GetValue("loc_test1");
        var value2 = _localizationService.GetValue("loc_test2");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(value1, Is.EqualTo("value1"));
            Assert.That(value2, Is.EqualTo("value2"));
        }
    }

    [Test]
    public void GetValue_ShouldReturnKey_WhenKeyDoesNotExist()
    {
        var key = "NON_EXISTENT_KEY";
        var value = _localizationService.GetValue(key);
        Assert.That(value, Is.EqualTo(key));
    }

    [Test]
    public void AddOrUpdateLocalisation_ShouldUpdateInMemoryValue()
    {
        var focusFilePath = Path.Combine(_testRunDirectory, "common", "national_focus", "test_focus.txt");
        var key = "NEW_KEY";
        var value = "新值";

        _localizationService.AddOrUpdateLocalisation(focusFilePath, GameLanguage.Chinese, key, value);

        var retrievedValue = _localizationService.GetValue(key);
        Assert.That(retrievedValue, Is.EqualTo(value));
    }

    [Test]
    public void SaveFocusTreeMessage_ShouldWriteNewFile_WithCorrectPathAndContent()
    {
        // Arrange
        var focusFileName = "new_focus_tree";
        var focusFilePath = Path.Combine(
            _testRunDirectory,
            "common",
            "national_focus",
            $"{focusFileName}.txt"
        );
        var key = "FOCUS_NAME";
        var value = "国策名称";

        // Act
        _localizationService.AddOrUpdateLocalisation(focusFilePath, GameLanguage.Chinese, key, value);

        // 发送保存消息触发写入
        _serviceProvider.GetRequiredService<IPublisher<SaveLocalizationMessage>>().Publish(new SaveLocalizationMessage());

        // Assert
        // 预期路径: <ModRoot>/localisation/simp_chinese/new_focus_tree.yml
        var expectedLocPath = Path.Combine(
            _testRunDirectory,
            "localisation",
            "simp_chinese",
            $"{focusFileName}_l_simp_chinese.yml"
        );

        Assert.That(File.Exists(expectedLocPath), Is.True, "本地化文件未创建");

        var content = File.ReadAllText(expectedLocPath);
        Assert.That(content, Does.Contain("l_simp_chinese:"));
        Assert.That(content, Does.Contain($" {key}: \"{value}\""));
    }

    [Test]
    public void SaveFocusTreeMessage_ShouldMergeWithExistingFile()
    {
        // Arrange
        // 模拟一个已存在的国策对应的本地化文件
        var focusFileName = "test"; // 对应 test.yml
        var focusFilePath = Path.Combine(
            _testRunDirectory,
            "common",
            "national_focus",
            $"{focusFileName}.txt"
        );

        // 确保 test.yml 已经存在 (在 Setup 中已复制)
        var existingLocPath = Path.Combine(
            _testRunDirectory,
            "localisation",
            "simp_chinese",
            $"{focusFileName}_l_simp_chinese.yml"
        );
        Assert.That(File.Exists(existingLocPath), Is.True);

        // Act
        // 更新一个现有的 key，并添加一个新的 key
        _localizationService.AddOrUpdateLocalisation(
            focusFilePath,
            GameLanguage.Chinese,
            "loc_test1",
            "updated_value"
        );
        _localizationService.AddOrUpdateLocalisation(
            focusFilePath,
            GameLanguage.Chinese,
            "new_key",
            "new_value"
        );

        _serviceProvider.GetRequiredService<IPublisher<SaveLocalizationMessage>>().Publish(new SaveLocalizationMessage());

        // Assert
        var content = File.ReadAllText(existingLocPath);

        // 验证旧值被更新
        Assert.That(content, Does.Contain("loc_test1: \"updated_value\""));
        // 验证未修改的旧值保留
        Assert.That(content, Does.Contain("loc_test2: \"value2\""));
        // 验证新值被添加
        Assert.That(content, Does.Contain("new_key: \"new_value\""));
    }
}
