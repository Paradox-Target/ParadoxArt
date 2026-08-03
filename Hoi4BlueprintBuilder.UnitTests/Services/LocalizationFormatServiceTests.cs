using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.UnitTests.Services;

[TestFixture(TestOf = typeof(LocalizationFormatService))]
public class LocalizationFormatServiceTests
{
    private string _testRunDirectory;
    private ServiceProvider _serviceProvider;
    private LocalizationFormatService _formatService;

    private static readonly string SourceLocPath = Path.Combine(
        TestApp.TestDataDirectory,
        "localisation",
        "simp_chinese",
        "test.yml"
    );

    private static readonly string SourceCoreGfxPath = Path.Combine(
        TestApp.TestDataDirectory,
        "interface",
        "core.gfx"
    );

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _testRunDirectory = TestHelper.CreateUniqueTempDirectory();
        TestContext.Out.WriteLine($"测试临时文件夹: {_testRunDirectory}");

        // 准备本地化文件
        var locDir = Path.Combine(_testRunDirectory, "localisation", "simp_chinese");
        Directory.CreateDirectory(locDir);
        File.Copy(SourceLocPath, Path.Combine(locDir, "test_l_simp_chinese.yml"));

        // 准备文本颜色文件 (interface/core.gfx)
        var interfaceDir = Path.Combine(_testRunDirectory, "interface");
        Directory.CreateDirectory(interfaceDir);
        File.Copy(SourceCoreGfxPath, Path.Combine(interfaceDir, "core.gfx"));

        var settingsService = new SettingsService
        {
            ModRootFolderPath = _testRunDirectory,
            GameRootFolderPath = _testRunDirectory,
            GameLanguage = GameLanguage.Chinese
        };
        var descriptorService = new GameModDescriptorService(
            new SettingsService { ModRootFolderPath = TestApp.TestDataDirectory }
        );

        var services = new ServiceCollection();
        services.AddSingleton(settingsService);
        services.AddSingleton(descriptorService);
        services.AddSingleton<GameResourcesWatcherService>();
        services.AddSingleton<GameResourcesPathService>();
        services.AddMessagePipe();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton(new ProjectConfigService { SupportedLanguages = [GameLanguage.Chinese] });
        services.AddSingleton<LocalizationTextColorsService>();
        services.AddSingleton<LocalizationFormatService>();
        _serviceProvider = services.BuildServiceProvider();
        _formatService = _serviceProvider.GetRequiredService<LocalizationFormatService>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
        if (Directory.Exists(_testRunDirectory))
        {
            Directory.Delete(_testRunDirectory, true);
        }
    }

    [Test]
    public void GetFormatText_WithPlaceholder_ShouldSubstituteValueInsideColorBlock()
    {
        // 测试字符串: §R-需要该团中至少有§!§H$NUM_BATTALIONS|H$个作战营§!§R。§!
        // 占位符 NUM_BATTALIONS 嵌套在 §H...§! 颜色块中, 且带有 |H 格式说明符
        // 期望结果: -需要该团中至少有5个作战营。
        string result = _formatService.GetFormatText(
            "DESIGNER_BLOCKED_BY_REGIMENT_BATTALIONS",
            "NUM_BATTALIONS",
            5
        );

        Assert.That(result, Is.EqualTo("-需要该团中至少有5个作战营。"));
    }

    [Test]
    public void GetFormatText_WithoutPlaceholder_ShouldSkipFormatSpecifier()
    {
        // 不提供占位符时, $NUM_BATTALIONS|H$ 作为格式说明符被跳过
        string result = _formatService.GetFormatText("DESIGNER_BLOCKED_BY_REGIMENT_BATTALIONS");

        Assert.That(result, Is.EqualTo("-需要该团中至少有个作战营。"));
    }

    // ===== 复杂嵌套测试用例 =====
    // 以下测试用例基于 HOI4 本体 designer_l_simp_chinese.yml 中的真实模式构造

    [Test]
    public void GetFormatText_MultiplePlaceholdersInsideOneColorBlock_ShouldSubstituteOnlyMatching()
    {
        // 模式来源: THEATER_GROUP_OFFENSIVE_TROUBLES
        // 值: §R$COUNT|H$场攻击战中有$BAD_COUNT|H$场处于劣势§!
        // 一个颜色块内含两个占位符, 仅替换 COUNT
        string result = _formatService.GetFormatText("COMPLEX_MULTI_PLACEHOLDER_IN_COLOR", "COUNT", 10);

        Assert.That(result, Is.EqualTo("10场攻击战中有场处于劣势"));
    }

    [Test]
    public void GetFormatText_MultiplePlaceholdersInsideOneColorBlock_ShouldSubstituteSecond()
    {
        // 同一字符串, 仅替换 BAD_COUNT
        string result = _formatService.GetFormatText("COMPLEX_MULTI_PLACEHOLDER_IN_COLOR", "BAD_COUNT", 3);

        Assert.That(result, Is.EqualTo("场攻击战中有3场处于劣势"));
    }

    [Test]
    public void GetFormatText_AdjacentPlaceholders_ShouldSubstituteOnlyMatching()
    {
        // 模式来源: AIR_VIEW_AVERAGE_MISSION_EFFICIENCY_FRIEND
        // 值: 效率：$VAL_HIGH|G%$$VAL_MID|H%$$VAL_LOW|R%$
        // 三个相邻占位符 (无颜色块包裹), 各带不同的颜色格式说明符
        string result = _formatService.GetFormatText("COMPLEX_ADJACENT_PLACEHOLDERS", "VAL_MID", 75);

        Assert.That(result, Is.EqualTo("效率：75"));
    }

    [Test]
    public void GetFormatText_AdjacentPlaceholders_WithoutPlaceholder_ShouldSkipAll()
    {
        // 不提供占位符时, 所有带 | 的占位符均被跳过
        string result = _formatService.GetFormatText("COMPLEX_ADJACENT_PLACEHOLDERS");

        Assert.That(result, Is.EqualTo("效率："));
    }

    [Test]
    public void GetFormatText_PlaceholdersInsideAndOutsideColorBlock_ShouldSubstituteInside()
    {
        // 模式来源: DIVISION_MODIFICATION_NEED_NOT_FILLED
        // 值: 需要额外$AMOUNT|H0$ $EQUIPMENT|H$（§R现有$STOCK_AMOUNT|H0^$可用§!）
        // 占位符分布在颜色块内外, 目标占位符在颜色块内, 带 |H0^ 复合格式说明符
        string result = _formatService.GetFormatText(
            "COMPLEX_PLACEHOLDER_IN_AND_OUT_COLOR",
            "STOCK_AMOUNT",
            100
        );

        Assert.That(result, Is.EqualTo("需要额外 （现有100可用）"));
    }

    [Test]
    public void GetFormatText_SequentialColorBlocks_ShouldSubstituteInSecondBlock()
    {
        // 模式来源: DESIGNER_BLOCKED_BY_REGIMENT_BATTALIONS 变体
        // 值: §R$COUNT|H$场§!中间文本§G$MAX|H$个§!
        // 两个不同颜色的颜色块, 各含一个占位符, 中间有普通文本
        string result = _formatService.GetFormatText("COMPLEX_SEQUENTIAL_COLOR_BLOCKS", "MAX", 20);

        Assert.That(result, Is.EqualTo("场中间文本20个"));
    }

    [Test]
    public void GetFormatText_SequentialColorBlocks_ShouldSubstituteInFirstBlock()
    {
        // 同一字符串, 替换第一个颜色块中的 COUNT
        string result = _formatService.GetFormatText("COMPLEX_SEQUENTIAL_COLOR_BLOCKS", "COUNT", 8);

        Assert.That(result, Is.EqualTo("8场中间文本个"));
    }

    [Test]
    public void GetFormatText_SamePlaceholderInsideAndOutsideColorBlock_ShouldSubstituteBoth()
    {
        // 值: 外层$VAL|0$文本§R内层$VAL|H$§!
        // 同名占位符分别出现在颜色块外和块内, 应同时替换
        string result = _formatService.GetFormatText("COMPLEX_SAME_PLACEHOLDER_IN_OUT", "VAL", 99);

        Assert.That(result, Is.EqualTo("外层99文本内层99"));
    }

    [Test]
    public void GetFormatText_DeepNestedThroughKeyReference_ShouldSubstituteInInnerColorBlock()
    {
        // 深层嵌套: 颜色块 → 本地化键引用 → 颜色块 → 占位符
        // COMPLEX_DEEP_NESTED_OUTER: §R前缀$COMPLEX_DEEP_NESTED_INNER$后缀§!
        // COMPLEX_DEEP_NESTED_INNER: §H$DEEP_VALUE|H$中§!
        // 解析路径: 外层§R → 解析$INNER$引用 → 内层§H → 替换$DEEP_VALUE|H$
        string result = _formatService.GetFormatText("COMPLEX_DEEP_NESTED_OUTER", "DEEP_VALUE", 42);

        Assert.That(result, Is.EqualTo("前缀42中后缀"));
    }

    [Test]
    public void GetFormatText_DeepNestedThroughKeyReference_WithoutPlaceholder_ShouldSkipInner()
    {
        // 不提供占位符时, 深层嵌套中的 $DEEP_VALUE|H$ 被跳过
        string result = _formatService.GetFormatText("COMPLEX_DEEP_NESTED_OUTER");

        Assert.That(result, Is.EqualTo("前缀中后缀"));
    }

    [Test]
    public void GetFormatText_IconAndPlaceholderInsideColorBlock_ShouldSkipIconAndSubstitute()
    {
        // 模式来源: DESIGNER_HQ_CP_COST_CHANGE
        // 值: §Y花费£command_power $OLD|0H$改为£command_power $NEW|0H$§!
        // 颜色块内含图标(£)和多个占位符
        string result = _formatService.GetFormatText("COMPLEX_ICON_AND_PLACEHOLDER", "NEW", 50);

        // 图标被跳过, OLD 被跳过 (含|), NEW 被替换
        Assert.That(result, Is.EqualTo("花费改为50"));
    }

    [Test]
    public void GetFormatText_MixedAll_ShouldSubstituteTargetAcrossBlocksAndReferences()
    {
        // 综合测试: 多颜色块 + 普通文本 + 占位符 + 本地化键引用
        // 值: §R$COUNT|H$场§!普通文本$RATE|0$§G$MAX|H$个§!$COMPLEX_DEEP_NESTED_INNER$
        // COMPLEX_DEEP_NESTED_INNER: §H$DEEP_VALUE|H$中§!
        // 替换 MAX → 解析路径跨越3个颜色块和1层键引用
        string result = _formatService.GetFormatText("COMPLEX_MIXED_ALL", "MAX", 30);

        // COUNT/RATE/DEEP_VALUE 均被跳过 (含|或不匹配), 仅 MAX 被替换
        Assert.That(result, Is.EqualTo("场普通文本30个中"));
    }

    [Test]
    public void GetFormatText_MixedAll_ShouldSubstituteDeepNestedPlaceholder()
    {
        // 同一综合字符串, 替换深层嵌套引用中的 DEEP_VALUE
        string result = _formatService.GetFormatText("COMPLEX_MIXED_ALL", "DEEP_VALUE", 7);

        // COUNT/RATE/MAX 被跳过, DEEP_VALUE 在深层引用中被替换
        Assert.That(result, Is.EqualTo("场普通文本个7中"));
    }

    // ===== Wiki 规范: $$ 转义 (https://hoi4.paradoxwikis.com/Localisation#Nesting_strings) =====
    // "Inputting a dollar sign itself. This is done by doubling the dollar sign in localisation,
    //  such as cost_tooltip: "This option costs $$100"."

    [Test]
    public void GetFormatText_EscapedDollar_PlainText_ShouldProduceLiteralDollar()
    {
        // 值: "价格$$100" → $$ 产生字面 $
        string result = _formatService.GetFormatText("ESCAPED_DOLLAR_PLAIN");

        Assert.That(result, Is.EqualTo("价格$100"));
    }

    [Test]
    public void GetFormatText_EscapedDollar_InsideColorBlock_ShouldProduceLiteralDollar()
    {
        // 值: "§R价格$$100§!" → 颜色块内的 $$ 也应被处理为字面 $
        // 这验证了 GetColorText 的修复: 即使没有占位符, 也应重新解析内部文本
        string result = _formatService.GetFormatText("ESCAPED_DOLLAR_IN_COLOR");

        Assert.That(result, Is.EqualTo("价格$100"));
    }

    [Test]
    public void GetFormatText_EscapedDollar_WithPlaceholder_ShouldDistinguishEscapeFromPlaceholder()
    {
        // 值: "$$100$VAL|0$元"
        // $$100 → 字面 "$100", $VAL|0$ → 占位符, 元 → 文本
        string result = _formatService.GetFormatText("ESCAPED_DOLLAR_WITH_PLACEHOLDER", "VAL", 50);

        Assert.That(result, Is.EqualTo("$10050元"));
    }

    [Test]
    public void GetFormatText_EscapedDollar_AdjacentPlaceholder_ShouldNotConfuseBoundary()
    {
        // 值: "$$100$VAL|0$"
        // $$100 → 字面 "$100", $VAL|0$ → 占位符
        string result = _formatService.GetFormatText(
            "ESCAPED_DOLLAR_ADJACENT_PLACEHOLDER",
            "VAL",
            50
        );

        Assert.That(result, Is.EqualTo("$10050"));
    }
}
