using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.UnitTests;

[TestFixture(TestOf = typeof(FocusNodeHelper))]
public sealed class FocusNodeParserTests
{
    private const string TestDataPath = "TestData/FocusNodeParserTests.txt";
    private string _fullTestDataPath;
    private Node _rootNode = null!;

    [SetUp]
    public void Setup()
    {
        // 获取完整的测试数据路径
        _fullTestDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, TestDataPath);
        Assert.That(File.Exists(_fullTestDataPath), Is.True, $"测试数据文件不存在: {_fullTestDataPath}");

        // 解析测试数据文件
        var parseResult = TextParser.TryParse(_fullTestDataPath, out _rootNode!, out var errorMessage);
        Assert.That(parseResult, Is.True, $"解析测试数据文件失败: {errorMessage}");
        Assert.That(_rootNode, Is.Not.Null, "解析结果为空");
    }

    [Test]
    public void GetFocusNodesFromAstRootNode_ShouldReturnAllFocusNodes()
    {
        // 执行测试
        var focusNodes = FocusNodeHelper.GetFocusNodesFromAstRootNode(_rootNode);

        // 验证结果
        Assert.That(focusNodes, Is.Not.Null, "返回的国策节点集合为空");
        Assert.That(focusNodes.Count(), Is.EqualTo(3), "返回的国策节点数量不正确");
    }

    [Test]
    public void GetAllNodesFromAst_ShouldParseAllNodesCorrectly()
    {
        // 执行测试
        var (nodes, filePaths) = FocusNodeHelper.GetAllNodesFromAst(_fullTestDataPath, _rootNode);

        // 验证结果
        Assert.That(nodes, Is.Not.Null, "返回的节点字典为空");
        Assert.That(nodes.Count, Is.EqualTo(3), "解析的节点数量不正确");

        // 验证文件路径
        Assert.That(filePaths, Is.Not.Null, "返回的文件路径集合为空");
        Assert.That(filePaths, Contains.Item(_fullTestDataPath), "未包含测试数据文件路径");

        // 验证节点属性
        Assert.That(nodes, Contains.Key("test_focus_1"), "未找到test_focus_1节点");
        Assert.That(nodes, Contains.Key("test_focus_2"), "未找到test_focus_2节点");
        Assert.That(nodes, Contains.Key("test_focus_3"), "未找到test_focus_3节点");

        // 验证第一个节点的属性
        var focus1 = nodes["test_focus_1"];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(focus1.Id, Is.EqualTo("test_focus_1"), "节点ID不正确");
            Assert.That(focus1.Icon, Is.EqualTo("GFX_goal_generic_intelligence_exchange"), "节点图标不正确");
            Assert.That(focus1.RawPosition.X, Is.EqualTo(2), "节点X坐标不正确");
            Assert.That(focus1.RawPosition.Y, Is.Zero, "节点Y坐标不正确");
            Assert.That(focus1.Cost, Is.EqualTo(10), "节点成本不正确");
            Assert.That(focus1.Type, Is.EqualTo(FocusType.Normal), "节点类型不正确");
            Assert.That(focus1.Prerequisite, Is.Empty, "节点先决条件不正确");
            Assert.That(focus1.RelativePosition, Is.Null, "节点相对位置不正确");
            Assert.That(focus1.MutuallyExclusive, Is.Empty, "节点互斥关系不正确");
            Assert.That(focus1.CompletionReward, Does.Contain("add_political_power = 100"), "节点完成奖励不正确");
            Assert.That(focus1.Offsets, Has.Count.EqualTo(2));
            Assert.That(((IFocusTrigger)focus1.Offsets.First()).DisplayContent, Is.EqualTo("tag = tst"));
            Assert.That(focus1.Offsets.First().Offset, Is.EqualTo(new FocusPoint(1, 2)));
            Assert.That(focus1.AllowBranch, Is.Not.Null);
            Assert.That(
                ((IFocusTrigger)focus1.AllowBranch!).DisplayContent,
                Is.EqualTo("has_dlc = \"test\"")
            );
        }
    }

    [Test]
    public void GetAllNodesFromAst_ShouldProcessNodeRelationsCorrectly()
    {
        // 执行测试
        var (nodes, _) = FocusNodeHelper.GetAllNodesFromAst(_fullTestDataPath, _rootNode);

        // 验证先决条件关系
        var focus2 = nodes["test_focus_2"];
        Assert.That(focus2.Prerequisite, Is.Not.Null, "先决条件集合为空");
        Assert.That(focus2.Prerequisite.Count, Is.EqualTo(1), "先决条件数量不正确");
        Assert.That(focus2.Prerequisite[0].Count, Is.EqualTo(1), "先决条件列表数量不正确");
        Assert.That(focus2.Prerequisite[0][0].Id, Is.EqualTo("test_focus_1"), "先决条件节点ID不正确");

        // 验证相对位置关系
        Assert.That(focus2.RelativePosition, Is.Not.Null, "相对位置为空");
        Assert.That(focus2.RelativePosition.Id, Is.EqualTo("test_focus_1"), "相对位置节点ID不正确");

        // 验证互斥关系
        Assert.That(focus2.MutuallyExclusive, Is.Not.Null, "互斥节点集合为空");
        Assert.That(focus2.MutuallyExclusive.Count, Is.EqualTo(1), "互斥节点数量不正确");
        Assert.That(focus2.MutuallyExclusive[0].Id, Is.EqualTo("test_focus_3"), "互斥节点ID不正确");
        Assert.That(focus2.CompletionReward, Is.Empty);

        // 验证其他属性
        Assert.That(focus2.Offsets, Is.Empty);
        Assert.That(focus2.AllowBranch, Is.Null);

        // 验证focus3的互斥关系
        var focus3 = nodes["test_focus_3"];
        Assert.That(focus3.MutuallyExclusive, Is.Not.Null, "focus3互斥节点集合为空");
        Assert.That(focus3.MutuallyExclusive.Count, Is.EqualTo(1), "focus3互斥节点数量不正确");
        Assert.That(focus3.MutuallyExclusive[0].Id, Is.EqualTo("test_focus_2"), "focus3互斥节点ID不正确");
        Assert.That(focus3.CompletionReward, Is.Empty);
        Assert.That(focus3.Offsets, Is.Empty);
        Assert.That(focus3.AllowBranch, Is.Null);
    }

    [Test]
    public void CreateAstNodeFromEditorModel_ShouldGenerateCorrectAst()
    {
        // 首先获取解析后的节点
        var (nodes, _) = FocusNodeHelper.GetAllNodesFromAst(_fullTestDataPath, _rootNode);
        var originalNode = nodes["test_focus_1"];

        // 使用模型创建AST节点
        var generatedNode = FocusNodeHelper.CreateAstNodeFromEditorModel(originalNode);

        // 验证生成的AST节点
        Assert.That(generatedNode, Is.Not.Null, "生成的AST节点为空");
        Assert.That(generatedNode.Key, Is.EqualTo("focus"), "AST节点键不正确");
    }

    [Test]
    public void CreateAstNodeFromEditorModel_ShouldHandleComplexRelations()
    {
        // 首先获取解析后的节点
        var (nodes, _) = FocusNodeHelper.GetAllNodesFromAst(_fullTestDataPath, _rootNode);
        var originalNode = nodes["test_focus_2"];

        // 使用模型创建AST节点
        var generatedNode = FocusNodeHelper.CreateAstNodeFromEditorModel(originalNode);

        // 验证生成的AST节点
        Assert.That(generatedNode, Is.Not.Null, "生成的AST节点为空");
        Assert.That(generatedNode.Key, Is.EqualTo("focus"), "AST节点键不正确");

        // 验证相对位置
        var relativePositionLeaf = generatedNode.Leaves.FirstOrDefault(l => l.Key == "relative_position_id");
        Assert.That(relativePositionLeaf, Is.Not.Null, "未找到relative_position_id属性");
        Assert.That(relativePositionLeaf.ValueText, Is.EqualTo("test_focus_1"), "relative_position_id值不正确");

        // 验证互斥关系
        var mutuallyExclusiveNode = generatedNode.Nodes.FirstOrDefault(n => n.Key == "mutually_exclusive");
        Assert.That(mutuallyExclusiveNode, Is.Not.Null, "未找到mutually_exclusive节点");
        var meFocusLeaf = mutuallyExclusiveNode.Leaves.FirstOrDefault(l => l.Key == "focus");
        Assert.That(meFocusLeaf, Is.Not.Null, "mutually_exclusive中未找到focus属性");
        Assert.That(meFocusLeaf.ValueText, Is.EqualTo("test_focus_3"), "mutually_exclusive focus值不正确");

        // 验证先决条件
        var prerequisiteNode = generatedNode.Nodes.FirstOrDefault(n => n.Key == "prerequisite");
        Assert.That(prerequisiteNode, Is.Not.Null, "未找到prerequisite节点");
        var preFocusLeaf = prerequisiteNode.Leaves.FirstOrDefault(l => l.Key == "focus");
        Assert.That(preFocusLeaf, Is.Not.Null, "prerequisite中未找到focus属性");
        Assert.That(preFocusLeaf.ValueText, Is.EqualTo("test_focus_1"), "prerequisite focus值不正确");
    }

    [Test]
    public void ChildrenTest()
    {
        // 首先获取解析后的节点
        var (nodes, _) = FocusNodeHelper.GetAllNodesFromAst(_fullTestDataPath, _rootNode);
        var focus1 = nodes["test_focus_1"];
        var focus2 = nodes["test_focus_2"];
        var focus3 = nodes["test_focus_3"];

        // 验证Children关系
        Assert.That(focus1.Children, Is.EquivalentTo([focus2, focus3]), "focus1的Children中未包含focus2, focus3");
        Assert.That(focus1.Children.Count, Is.EqualTo(2), "focus1的Children数量不正确");
        Assert.That(focus2.Children, Is.Empty, "focus2的Children不应包含任何节点");
        Assert.That(focus3.Children, Is.Empty, "focus3的Children不应包含任何节点");
    }
}
