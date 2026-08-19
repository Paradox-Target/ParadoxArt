using Hoi4BlueprintBuilder.Core.Helpers;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.UnitTests.Helpers;

[TestFixture]
public class DivisionTemplateHelperTests
{
    [Test]
    public void GetDivisionTemplates_ShouldReturnAllRootLevelTemplates()
    {
        var text =
            @"
division_template = {
    name = ""Infantry Division""
    regiments = {
        infantry = { x = 0 y = 0 }
    }
}
units = {
    division = {
        name = ""1st Infantry Division""
        division_template = ""Infantry Division""
    }
}
division_template = {
    name = ""Armored Division""
    priority = 2
    is_locked = yes
}
";
        TextParser.TryParse(string.Empty, text, out var root, out var error);

        var templates = DivisionTemplateHelper.GetDivisionTemplates(root).ToList();

        Assert.That(templates, Has.Count.EqualTo(2));
        Assert.That(templates[0].LeavesValue.First().ValueText, Is.EqualTo("Infantry Division"));
        Assert.That(templates[1].LeavesValue.First().ValueText, Is.EqualTo("Armored Division"));
    }

    [Test]
    public void GetDivisionTemplates_ShouldIgnoreTemplatesNestedInOtherNodes()
    {
        var text =
            @"
country = {
    division_template = {
        name = ""Nested Division""
    }
}
division_template = {
    name = ""Root Division""
}
";
        TextParser.TryParse(string.Empty, text, out var root, out var error);

        var templates = DivisionTemplateHelper.GetDivisionTemplates(root).ToList();

        Assert.That(templates, Has.Count.EqualTo(1));
        Assert.That(templates[0].LeavesValue.First().ValueText, Is.EqualTo("Root Division"));
    }

    [Test]
    public void GetDivisionTemplates_ShouldReturnEmpty_WhenNoTemplates()
    {
        var text = "units = { division = { name = \"Only Unit\" } }";
        TextParser.TryParse(string.Empty, text, out var root, out var error);

        var templates = DivisionTemplateHelper.GetDivisionTemplates(root).ToList();

        Assert.That(templates, Is.Empty);
    }
}
