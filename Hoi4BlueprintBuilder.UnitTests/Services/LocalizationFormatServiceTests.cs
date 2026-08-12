using Avalonia.Media;
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
        services.AddSingleton(new LocalizationKeyMappingService(new StringReader("Raw Key,Mapping Key\r\n")));
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
        string result = _formatService.GetFormatText("ESCAPED_DOLLAR_ADJACENT_PLACEHOLDER", "VAL", 50);

        Assert.That(result, Is.EqualTo("$10050"));
    }

    // ===== 颜色相关测试 =====
    // 颜色值来自 TestData/interface/core.gfx 中的 textcolors 定义

    private static readonly Color ColorRed = Color.FromRgb(255, 50, 50); // R
    private static readonly Color ColorGreen = Color.FromRgb(0, 159, 3); // G
    private static readonly Color ColorBlue = Color.FromRgb(0, 0, 255); // B
    private static readonly Color ColorGold = Color.FromRgb(255, 189, 0); // H / Y

    [Test]
    public void GetFormatTextInfo_SimpleColorBlock_ShouldAssignColor()
    {
        // §R红色文本§! → 整段为红色
        var result = _formatService.GetFormatTextInfo("§R红色文本§!");

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.First().DisplayText, Is.EqualTo("红色文本"));
            Assert.That(result.First().Color, Is.EqualTo(ColorRed));
        });
    }

    [Test]
    public void GetFormatTextInfo_MultipleColorBlocks_ShouldAssignEachColor()
    {
        // §R红§!§G绿§!§B蓝§! → 三段不同颜色
        var result = _formatService.GetFormatTextInfo("§R红§!§G绿§!§B蓝§!");

        var list = result.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(list, Has.Count.EqualTo(3));
            Assert.That(list[0].DisplayText, Is.EqualTo("红"));
            Assert.That(list[0].Color, Is.EqualTo(ColorRed));
            Assert.That(list[1].DisplayText, Is.EqualTo("绿"));
            Assert.That(list[1].Color, Is.EqualTo(ColorGreen));
            Assert.That(list[2].DisplayText, Is.EqualTo("蓝"));
            Assert.That(list[2].Color, Is.EqualTo(ColorBlue));
        });
    }

    [Test]
    public void GetFormatTextInfo_TextAroundColor_ShouldHaveNullColorForPlainText()
    {
        // 前§R红色§!后 → 黑(null) + 红 + 黑(null)
        var result = _formatService.GetFormatTextInfo("前§R红色§!后");

        var list = result.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(list, Has.Count.EqualTo(3));
            Assert.That(list[0].DisplayText, Is.EqualTo("前"));
            Assert.That(list[0].Color, Is.Null);
            Assert.That(list[1].DisplayText, Is.EqualTo("红色"));
            Assert.That(list[1].Color, Is.EqualTo(ColorRed));
            Assert.That(list[2].DisplayText, Is.EqualTo("后"));
            Assert.That(list[2].Color, Is.Null);
        });
    }

    [Test]
    public void GetFormatTextInfo_NoColorBlocks_ShouldAllHaveNullColor()
    {
        // 普通文本无颜色 → 单段 null 颜色
        var result = _formatService.GetFormatTextInfo("普通文本无颜色");

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.First().DisplayText, Is.EqualTo("普通文本无颜色"));
            Assert.That(result.First().Color, Is.Null);
        });
    }

    [Test]
    public void GetFormatTextInfo_UnknownColorCode_ShouldFallBackToNullColor()
    {
        // §Z未知§! → Z 不在 core.gfx 中, 颜色为 null, 文本保留 "Z未知"
        var result = _formatService.GetFormatTextInfo("§Z未知§!");

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.First().DisplayText, Is.EqualTo("Z未知"));
            Assert.That(result.First().Color, Is.Null);
        });
    }

    [Test]
    public void GetFormatTextInfo_EmptyColorBlock_ShouldProduceNoItems()
    {
        // §R§! → 颜色块内容为空, 解析后不产生任何 TextFormatInfo
        var result = _formatService.GetFormatTextInfo("§R§!");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetFormatTextInfoByKey_PlaceholderWithColorFormatSpecifier_ShouldUseSpecifierColor()
    {
        // §R$NUM|H$个§! with NUM=5
        // $NUM|H$ 的格式说明符 H 是颜色码 → 占位符值 "5" 使用金色 (H), 而非外层红色
        // "个" 无自身颜色 → 使用外层红色
        var result = _formatService.GetFormatTextInfoByKey("COLOR_PLACEHOLDER_FORMAT_H", "NUM", 5);

        var list = result.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(list, Has.Count.EqualTo(2));
            Assert.That(list[0].DisplayText, Is.EqualTo("5"));
            Assert.That(list[0].Color, Is.EqualTo(ColorGold));
            Assert.That(list[1].DisplayText, Is.EqualTo("个"));
            Assert.That(list[1].Color, Is.EqualTo(ColorRed));
        });
    }

    [Test]
    public void GetFormatTextInfoByKey_PlaceholderWithNonColorFormatSpecifier_ShouldUseOuterColor()
    {
        // §R$NUM|0$§! with NUM=5
        // 格式说明符 "0" 不是颜色码 → 占位符值 "5" 颜色为 null → 回退到外层红色
        var result = _formatService.GetFormatTextInfoByKey("COLOR_PLACEHOLDER_FORMAT_0", "NUM", 5);

        var list = result.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(list, Has.Count.EqualTo(1));
            Assert.That(list[0].DisplayText, Is.EqualTo("5"));
            Assert.That(list[0].Color, Is.EqualTo(ColorRed));
        });
    }

    [Test]
    public void GetFormatTextInfo_NestedKeyReference_ShouldPreserveInnerColor()
    {
        // §R前缀$COLOR_INNER_GREEN$后缀§! where COLOR_INNER_GREEN = §G绿色§!
        // 外层 §R 红色, 内层 §G 绿色
        // 内层颜色优先: "前缀"→红, "绿色"→绿 (内层颜色保留), "后缀"→红
        var result = _formatService.GetFormatTextInfo("§R前缀$COLOR_INNER_GREEN$后缀§!");

        var list = result.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(list, Has.Count.EqualTo(3));
            Assert.That(list[0].DisplayText, Is.EqualTo("前缀"));
            Assert.That(list[0].Color, Is.EqualTo(ColorRed));
            Assert.That(list[1].DisplayText, Is.EqualTo("绿色"));
            Assert.That(list[1].Color, Is.EqualTo(ColorGreen));
            Assert.That(list[2].DisplayText, Is.EqualTo("后缀"));
            Assert.That(list[2].Color, Is.EqualTo(ColorRed));
        });
    }

    [Test]
    public void GetFormatTextInfoByKey_DesignerBlocked_ShouldVerifyFullColorLayout()
    {
        // §R-需要该团中至少有§!§H$NUM_BATTALIONS|H$个作战营§!§R。§! with NUM_BATTALIONS=5
        // 段1: "-需要该团中至少有" → 红 (§R)
        // 段2: "5" → 金 (占位符 |H 格式说明符颜色, 优先于外层 §H)
        // 段3: "个作战营" → 金 (外层 §H)
        // 段4: "。" → 红 (§R)
        var result = _formatService.GetFormatTextInfoByKey(
            "DESIGNER_BLOCKED_BY_REGIMENT_BATTALIONS",
            "NUM_BATTALIONS",
            5
        );

        var list = result.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(list, Has.Count.EqualTo(4));
            Assert.That(list[0].DisplayText, Is.EqualTo("-需要该团中至少有"));
            Assert.That(list[0].Color, Is.EqualTo(ColorRed));
            Assert.That(list[1].DisplayText, Is.EqualTo("5"));
            Assert.That(list[1].Color, Is.EqualTo(ColorGold));
            Assert.That(list[2].DisplayText, Is.EqualTo("个作战营"));
            Assert.That(list[2].Color, Is.EqualTo(ColorGold));
            Assert.That(list[3].DisplayText, Is.EqualTo("。"));
            Assert.That(list[3].Color, Is.EqualTo(ColorRed));
        });
    }

    [Test]
    public void GetFormatTextInfoByKey_KeyNotFound_ShouldReturnKeyWithNullColor()
    {
        var result = _formatService.GetFormatTextInfoByKey("NON_EXISTENT_KEY", "NUM", 1);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.First().DisplayText, Is.EqualTo("NON_EXISTENT_KEY"));
            Assert.That(result.First().Color, Is.Null);
        });
    }

    [Test]
    public void NotExistFormatSpecifierTest()
    {
        var result = _formatService.GetFormatTextInfoByKey("COLOR_PLACEHOLDER_FORMAT", "NUM", 6);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result.First().DisplayText, Is.EqualTo("6"));
            Assert.That(result.First().Color, Is.EqualTo(ColorRed));
        });
    }
}
