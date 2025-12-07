using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;

namespace Hoi4BlueprintEditor.UnitTests;

public class TestNodeScript
{
    [Test]
    public static void TestCreateNode()
    {
        // var node = Node.Create("test string");
        // var value = ValueClause.Create()
        // node.ValueClauses
        // var a = node.ToScript();
        string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var folder = Path.Combine(path, "Paradox Interactive", "Hearts of Iron IV", "mod", "新建mod");
        var filepath = Path.Combine(folder, "descriptor.mod");
        var suc = TextParser.TryParse(filepath, out var node, out var errors);
        var n = node;

        node = Node.Create("title");
        node.AddLeafString("version", "1.16");
        node.AddLeafString("name", "画皮(0.1)");
        var tag = Node.Create("tag");
        var leaf = LeafValue.Create(Types.Value.NewString("Alternative History"));
        tag.AddChild(leaf);
        node.AddChild(tag);
        var str = node.ToScript();
    }
}
