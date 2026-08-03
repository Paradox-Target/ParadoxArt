using Hoi4BlueprintBuilder.Core.Infrastructure.Parser;

namespace Hoi4BlueprintBuilder.UnitTests.Infrastructure;

[TestFixture(TestOf = typeof(LocalizationFormatParser))]
public class LocalizationFormatParserTests
{
    [Test]
    public void TryParse_ShouldParseColorBlocksWithNestedPlaceholder()
    {
        // 测试字符串: §R-需要该团中至少有§!§H$NUM_BATTALIONS|H$个作战营§!§R。§!
        // 期望解析为 3 个 TextWithColor 段:
        //   1. "R-需要该团中至少有"
        //   2. "H$NUM_BATTALIONS|H$个作战营"  (包含嵌套占位符)
        //   3. "R。"
        const string input = "§R-需要该团中至少有§!§H$NUM_BATTALIONS|H$个作战营§!§R。§!";

        bool success = LocalizationFormatParser.TryParse(input, out var formats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(formats, Is.Not.Null);
        }

        var list = formats!.ToList();
        Assert.That(list, Has.Count.EqualTo(3), "应解析为 3 个段");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].Type, Is.EqualTo(LocalizationFormatType.TextWithColor));
            Assert.That(list[0].Text, Is.EqualTo("R-需要该团中至少有"));

            Assert.That(list[1].Type, Is.EqualTo(LocalizationFormatType.TextWithColor));
            Assert.That(list[1].Text, Is.EqualTo("H$NUM_BATTALIONS|H$个作战营"));

            Assert.That(list[2].Type, Is.EqualTo(LocalizationFormatType.TextWithColor));
            Assert.That(list[2].Text, Is.EqualTo("R。"));
        }
    }

    [Test]
    public void TryParse_ShouldParsePlaceholderInsideColorBlock_WhenReParsed()
    {
        // 模拟 GetColorText 中对颜色块内部文本的二次解析
        // 颜色块 "H$NUM_BATTALIONS|H$个作战营" 去除颜色码 'H' 后的内部文本
        const string innerText = "$NUM_BATTALIONS|H$个作战营";

        bool success = LocalizationFormatParser.TryParse(innerText, out var formats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(formats, Is.Not.Null);
        }

        var list = formats!.ToList();
        Assert.That(list, Has.Count.EqualTo(2), "内部文本应解析为 2 个段 (占位符 + 文本)");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].Type, Is.EqualTo(LocalizationFormatType.Placeholder));
            Assert.That(list[0].Text, Is.EqualTo("NUM_BATTALIONS|H"));

            Assert.That(list[1].Type, Is.EqualTo(LocalizationFormatType.Text));
            Assert.That(list[1].Text, Is.EqualTo("个作战营"));
        }
    }

    // ===== Wiki 规范: $$ 转义 (https://hoi4.paradoxwikis.com/Localisation#Nesting_strings) =====
    // "Inputting a dollar sign itself. This is done by doubling the dollar sign in localisation,
    //  such as cost_tooltip: "This option costs $$100"."

    [Test]
    public void TryParse_ShouldParseEscapedDollarSign_AsText()
    {
        // $$ 应被解析为文本 "$", 而非占位符起始
        // Wiki: "This option costs $$100"
        // TextParser 的 AtLeastOnceString() 会将 $$ 转义与相邻普通字符合并为单个 Text 段
        const string input = "价格$$100";

        bool success = LocalizationFormatParser.TryParse(input, out var formats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(formats, Is.Not.Null);
        }

        var list = formats!.ToList();
        Assert.That(list, Has.Count.EqualTo(1), "应解析为单个 Text 段 ($$ 转义与相邻文本合并)");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].Type, Is.EqualTo(LocalizationFormatType.Text));
            // $$ 被转换为 $, 与前后文本合并为一个字符串
            Assert.That(list[0].Text, Is.EqualTo("价格$100"));
        }
    }

    [Test]
    public void TryParse_ShouldParseEscapedDollarAdjacentToPlaceholder()
    {
        // $$100$VAL|0$ — $$ 产生字面 $, 随后 100 为文本, $VAL|0$ 为占位符
        // 验证 $$ 转义与占位符边界正确区分
        const string input = "$$100$VAL|0$";

        bool success = LocalizationFormatParser.TryParse(input, out var formats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(formats, Is.Not.Null);
        }

        var list = formats!.ToList();
        Assert.That(list, Has.Count.EqualTo(2), "应解析为 Text + Placeholder");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].Type, Is.EqualTo(LocalizationFormatType.Text));
            Assert.That(list[0].Text, Is.EqualTo("$100"));

            Assert.That(list[1].Type, Is.EqualTo(LocalizationFormatType.Placeholder));
            Assert.That(list[1].Text, Is.EqualTo("VAL|0"));
        }
    }

    [Test]
    public void TryParse_ShouldParseAdjacentPlaceholders_WithoutConfusingBoundaryAsEscape()
    {
        // $A|H$$B|H$ — 两个相邻占位符之间的 $$ 是第一个占位符的结束 $ 和第二个的起始 $
        // 不应被误解为 $$ 转义
        const string input = "$A|H$$B|H$";

        bool success = LocalizationFormatParser.TryParse(input, out var formats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(formats, Is.Not.Null);
        }

        var list = formats!.ToList();
        Assert.That(list, Has.Count.EqualTo(2), "应解析为 2 个 Placeholder, 不含 Text");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].Type, Is.EqualTo(LocalizationFormatType.Placeholder));
            Assert.That(list[0].Text, Is.EqualTo("A|H"));

            Assert.That(list[1].Type, Is.EqualTo(LocalizationFormatType.Placeholder));
            Assert.That(list[1].Text, Is.EqualTo("B|H"));
        }
    }

    // ===== Wiki 规范: \n 换行 (https://hoi4.paradoxwikis.com/Localisation#Special_characters) =====
    // "newlines are marked using \n (Note that this is a backslash rather than a regular slash)"

    [Test]
    public void TryParse_ShouldParseNewlineEscape_AsNewlineCharacter()
    {
        // \n (反斜杠+n) 应被解析为换行符
        const string input = "第一行\\n第二行";

        bool success = LocalizationFormatParser.TryParse(input, out var formats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(formats, Is.Not.Null);
        }

        var list = formats!.ToList();
        Assert.That(list, Has.Count.EqualTo(1), "应解析为单个 Text 段");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].Type, Is.EqualTo(LocalizationFormatType.Text));
            // \n 应被转换为实际换行符
            Assert.That(list[0].Text, Is.EqualTo("第一行\n第二行"));
        }
    }

    // ===== Wiki 规范: £ 文本图标 (https://hoi4.paradoxwikis.com/Localisation#Text_icons) =====
    // "Icons can be displayed within strings using the £ notation."
    // "If the sprite of the text icon is made out of multiple frames,
    //  then the specified frame can be used in localisation as £icon_name|1"

    [Test]
    public void TryParse_ShouldParseIcon_WithSpaceTerminator()
    {
        // £command_power 后跟空格 — 空格为终止符, 被消耗
        const string input = "£command_power 后续文本";

        bool success = LocalizationFormatParser.TryParse(input, out var formats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(formats, Is.Not.Null);
        }

        var list = formats!.ToList();
        Assert.That(list, Has.Count.EqualTo(2), "应解析为 Icon + Text");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].Type, Is.EqualTo(LocalizationFormatType.Icon));
            Assert.That(list[0].Text, Is.EqualTo("command_power"));

            Assert.That(list[1].Type, Is.EqualTo(LocalizationFormatType.Text));
            Assert.That(list[1].Text, Is.EqualTo("后续文本"));
        }
    }

    [Test]
    public void TryParse_ShouldParseIconWithFrameSpecifier()
    {
        // Wiki: £icon_name|1 — 多帧图标指定帧
        // 图标解析器以空格/!/£ 为终止符, |1 包含在图标名中
        const string input = "£icon_name|1 ";

        bool success = LocalizationFormatParser.TryParse(input, out var formats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(formats, Is.Not.Null);
        }

        var list = formats!.ToList();
        Assert.That(list, Has.Count.EqualTo(1), "应解析为单个 Icon");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].Type, Is.EqualTo(LocalizationFormatType.Icon));
            // |1 包含在图标名中 (当前实现: 图标名 = 终止符前的所有字符)
            Assert.That(list[0].Text, Is.EqualTo("icon_name|1"));
        }
    }
}

