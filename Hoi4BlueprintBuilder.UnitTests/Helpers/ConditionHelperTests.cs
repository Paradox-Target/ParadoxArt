using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.UnitTests.Helpers;

[TestFixture(TestOf = typeof(ConditionHelper))]
public sealed class ConditionHelperTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// 从 HOI4 脚本文本中解析出第一个顶层 Node（即 "trigger" 块本身）
    /// </summary>
    private static Node ParseTriggerNode(string triggerBlockText)
    {
        TextParser.TryParse(string.Empty, triggerBlockText, out var root, out _);
        Assert.That(root, Is.Not.Null, "解析 HOI4 脚本失败");
        return root!.Nodes.First();
    }

    private static HashSet<(string ScopeName, string NodeContent)> TrueSet(params (string, string)[] items) =>
        new(items);

    // =========================================================================
    // Evaluate — ConditionBool
    // =========================================================================

    [Test]
    public void Evaluate_ConditionBoolTrue_ReturnsTrue()
    {
        var result = ConditionHelper.Evaluate(new ConditionBool(true), TrueSet());
        Assert.That(result, Is.True);
    }

    [Test]
    public void Evaluate_ConditionBoolFalse_ReturnsFalse()
    {
        var result = ConditionHelper.Evaluate(new ConditionBool(false), TrueSet());
        Assert.That(result, Is.False);
    }

    // =========================================================================
    // Evaluate — ConditionLeaf
    // =========================================================================

    [Test]
    public void Evaluate_LeafPresentInTrueSet_ReturnsTrue()
    {
        var leaf = new ConditionLeaf("", "has_war = yes");
        var result = ConditionHelper.Evaluate(leaf, TrueSet(("", "has_war = yes")));
        Assert.That(result, Is.True);
    }

    [Test]
    public void Evaluate_LeafAbsentFromTrueSet_ReturnsFalse()
    {
        var leaf = new ConditionLeaf("", "has_war = yes");
        var result = ConditionHelper.Evaluate(leaf, TrueSet());
        Assert.That(result, Is.False);
    }

    [Test]
    public void Evaluate_LeafWithScope_MatchesByExactScopeAndContent()
    {
        var leaf = new ConditionLeaf("any_country", "tag = GER");
        var wrongScope = TrueSet(("", "tag = GER"));
        var correctScope = TrueSet(("any_country", "tag = GER"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ConditionHelper.Evaluate(leaf, wrongScope), Is.False, "不同 scope 不应匹配");
            Assert.That(ConditionHelper.Evaluate(leaf, correctScope), Is.True, "相同 scope 应匹配");
        }
    }

    // =========================================================================
    // Evaluate — ConditionFolder(And)
    // =========================================================================

    [Test]
    public void Evaluate_AndFolder_AllTrue_ReturnsTrue()
    {
        var folder = new ConditionFolder(
            ConditionFolderType.And,
            [new ConditionLeaf("", "has_war = yes"), new ConditionLeaf("", "tag = GER")]
        );
        var trueSet = TrueSet(("", "has_war = yes"), ("", "tag = GER"));
        Assert.That(ConditionHelper.Evaluate(folder, trueSet), Is.True);
    }

    [Test]
    public void Evaluate_AndFolder_OneFalse_ReturnsFalse()
    {
        var folder = new ConditionFolder(
            ConditionFolderType.And,
            [new ConditionLeaf("", "has_war = yes"), new ConditionLeaf("", "tag = GER")]
        );
        var trueSet = TrueSet(("", "has_war = yes")); // tag = GER 为 false
        Assert.That(ConditionHelper.Evaluate(folder, trueSet), Is.False);
    }

    // =========================================================================
    // Evaluate — ConditionFolder(Or)
    // =========================================================================

    [Test]
    public void Evaluate_OrFolder_AnyTrue_ReturnsTrue()
    {
        var folder = new ConditionFolder(
            ConditionFolderType.Or,
            [new ConditionLeaf("", "tag = GER"), new ConditionLeaf("", "tag = FRA")]
        );
        var trueSet = TrueSet(("", "tag = FRA")); // 只有 FRA 为 true
        Assert.That(ConditionHelper.Evaluate(folder, trueSet), Is.True);
    }

    [Test]
    public void Evaluate_OrFolder_AllFalse_ReturnsFalse()
    {
        var folder = new ConditionFolder(
            ConditionFolderType.Or,
            [new ConditionLeaf("", "tag = GER"), new ConditionLeaf("", "tag = FRA")]
        );
        Assert.That(ConditionHelper.Evaluate(folder, TrueSet()), Is.False);
    }

    // =========================================================================
    // Evaluate — ConditionFolder(AndNot)  i.e. HOI4 NOT = { A B }
    // =========================================================================

    [Test]
    public void Evaluate_AndNotFolder_AllFalse_ReturnsTrue()
    {
        // NOT = { A B }  全部为 false → 整体 true
        var folder = new ConditionFolder(
            ConditionFolderType.AndNot,
            [new ConditionLeaf("", "has_war = yes"), new ConditionLeaf("", "tag = GER")]
        );
        Assert.That(ConditionHelper.Evaluate(folder, TrueSet()), Is.True);
    }

    [Test]
    public void Evaluate_AndNotFolder_OneTrue_ReturnsFalse()
    {
        // NOT = { A B } 其中 A 为 true → 整体 false
        var folder = new ConditionFolder(
            ConditionFolderType.AndNot,
            [new ConditionLeaf("", "has_war = yes"), new ConditionLeaf("", "tag = GER")]
        );
        var trueSet = TrueSet(("", "has_war = yes"));
        Assert.That(ConditionHelper.Evaluate(folder, trueSet), Is.False);
    }

    // =========================================================================
    // Evaluate — ConditionFolder(OrNot)
    // =========================================================================

    [Test]
    public void Evaluate_OrNotFolder_AnyFalse_ReturnsTrue()
    {
        var folder = new ConditionFolder(
            ConditionFolderType.OrNot,
            [new ConditionLeaf("", "tag = GER"), new ConditionLeaf("", "tag = FRA")]
        );
        var trueSet = TrueSet(("", "tag = GER")); // FRA 为 false → 整体 true
        Assert.That(ConditionHelper.Evaluate(folder, trueSet), Is.True);
    }

    [Test]
    public void Evaluate_OrNotFolder_AllTrue_ReturnsFalse()
    {
        var folder = new ConditionFolder(
            ConditionFolderType.OrNot,
            [new ConditionLeaf("", "tag = GER"), new ConditionLeaf("", "tag = FRA")]
        );
        var trueSet = TrueSet(("", "tag = GER"), ("", "tag = FRA"));
        Assert.That(ConditionHelper.Evaluate(folder, trueSet), Is.False);
    }

    // =========================================================================
    // Evaluate — 嵌套复合表达式
    // =========================================================================

    [Test]
    public void Evaluate_NestedAndOr_EvaluatesCorrectly()
    {
        // AND( LeafA, OR(LeafB, LeafC) )
        var leafA = new ConditionLeaf("", "has_war = yes");
        var leafB = new ConditionLeaf("", "tag = GER");
        var leafC = new ConditionLeaf("", "tag = FRA");
        var orFolder = new ConditionFolder(ConditionFolderType.Or, [leafB, leafC]);
        var andFolder = new ConditionFolder(ConditionFolderType.And, [leafA, orFolder]);

        // A=true, B=false, C=true → AND(true, OR(false,true)) = true
        var trueSet = TrueSet(("", "has_war = yes"), ("", "tag = FRA"));
        Assert.That(ConditionHelper.Evaluate(andFolder, trueSet), Is.True);

        // A=true, B=false, C=false → AND(true, OR(false,false)) = false
        var trueSetOnlyA = TrueSet(("", "has_war = yes"));
        Assert.That(ConditionHelper.Evaluate(andFolder, trueSetOnlyA), Is.False);
    }

    // =========================================================================
    // ExtractConditionExpression — 从解析的 AST 节点提取
    // =========================================================================

    [Test]
    public void ExtractConditionExpression_EmptyTrigger_ReturnsConditionBoolTrue()
    {
        var triggerNode = ParseTriggerNode("trigger = { }");
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        Assert.That(expr, Is.InstanceOf<ConditionBool>());
        Assert.That(((ConditionBool)expr).Value, Is.True);
        Assert.That(collector, Is.Empty);
    }

    [Test]
    public void ExtractConditionExpression_SingleLeaf_ReturnsConditionLeaf()
    {
        var triggerNode = ParseTriggerNode("trigger = { has_war = yes }");
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        Assert.That(expr, Is.InstanceOf<ConditionLeaf>());
        var leaf = (ConditionLeaf)expr;
        Assert.That(leaf.ScopeName, Is.EqualTo(""));
        Assert.That(leaf.NodeContent, Is.EqualTo("has_war = yes"));
        Assert.That(collector, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExtractConditionExpression_MultipleLeaves_ReturnsAndFolder()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                has_war = yes
                tag = GER
            }
            """
        );
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        Assert.That(expr, Is.InstanceOf<ConditionFolder>());
        var folder = (ConditionFolder)expr;
        Assert.That(folder.Type, Is.EqualTo(ConditionFolderType.And));
        Assert.That(folder.Items, Has.Count.EqualTo(2));
        Assert.That(collector, Has.Count.EqualTo(2));
    }

    [Test]
    public void ExtractConditionExpression_AndBlock_ReturnsAndFolder()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                AND = {
                    has_war = yes
                    tag = GER
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        Assert.That(expr, Is.InstanceOf<ConditionFolder>());
        Assert.That(((ConditionFolder)expr).Type, Is.EqualTo(ConditionFolderType.And));
        Assert.That(collector, Has.Count.EqualTo(2));
    }

    [Test]
    public void ExtractConditionExpression_OrBlock_ReturnsOrFolder()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                OR = {
                    tag = GER
                    tag = FRA
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        Assert.That(expr, Is.InstanceOf<ConditionFolder>());
        Assert.That(((ConditionFolder)expr).Type, Is.EqualTo(ConditionFolderType.Or));
        Assert.That(collector, Has.Count.EqualTo(2));
    }

    [Test]
    public void ExtractConditionExpression_NotBlock_ReturnsAndNotFolder()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                NOT = {
                    has_war = yes
                    tag = FRA
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        Assert.That(expr, Is.InstanceOf<ConditionFolder>());
        Assert.That(((ConditionFolder)expr).Type, Is.EqualTo(ConditionFolderType.AndNot));
        Assert.That(collector, Has.Count.EqualTo(2));
    }

    [Test]
    public void ExtractConditionExpression_ScopeKeyword_CreatesNewScopeName()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                any_country = {
                    tag = GER
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        // 应当得到一个叶子条件, scope 为 "any_country"
        Assert.That(expr, Is.InstanceOf<ConditionLeaf>());
        var leaf = (ConditionLeaf)expr;
        Assert.That(leaf.ScopeName, Is.EqualTo("any_country"));
        Assert.That(leaf.NodeContent, Is.EqualTo("tag = GER"));
        Assert.That(collector, Has.Count.EqualTo(1));
        Assert.That(collector[0].ScopeName, Is.EqualTo("any_country"));
    }

    [Test]
    public void ExtractConditionExpression_NestedScope_AppendsScopeNames()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                any_country = {
                    any_state = {
                        is_core_of = GER
                    }
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        Assert.That(collector, Has.Count.EqualTo(1));
        Assert.That(collector[0].ScopeName, Is.EqualTo("any_country.any_state"));
    }

    [Test]
    public void ExtractConditionExpression_LimitBlock_UsesSameScope()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                limit = {
                    has_war = yes
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        // limit 透明递归, scope 不变
        Assert.That(expr, Is.InstanceOf<ConditionLeaf>());
        Assert.That(((ConditionLeaf)expr).ScopeName, Is.EqualTo(""));
        Assert.That(collector, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExtractConditionExpression_CollectorDeduplicates_SameCondition()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                OR = {
                    has_war = yes
                    has_war = yes
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        // 两条完全相同的 leaf, collector 应只保留一条
        Assert.That(collector, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExtractConditionExpression_CollectorPreexistingItem_NoDuplicate()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                has_war = yes
            }
            """
        );
        // 预先往 collector 中放一个相同条件
        var collector = new List<ConditionItem>
        {
            new ConditionLeafItem("", new Leaf("has_war", Types.Value.NewBool(true), Types.Operator.Equals))
        };

        ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        Assert.That(collector, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExtractConditionExpression_WithInitialScope_PrefixesNestedScope()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                any_country = {
                    tag = GER
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        // 传入初始 scope "ROOT"
        ConditionHelper.ExtractConditionExpression(triggerNode, "ROOT", collector);

        Assert.That(collector, Has.Count.EqualTo(1));
        Assert.That(collector[0].ScopeName, Is.EqualTo("ROOT.any_country"));
    }

    [Test]
    public void ExtractConditionExpression_IfBlock_ExtractsContainedConditions()
    {
        var triggerNode = ParseTriggerNode(
            """
            trigger = {
                if = {
                    limit = { has_war = yes }
                    tag = GER
                }
            }
            """
        );
        var collector = new List<ConditionItem>();

        var expr = ConditionHelper.ExtractConditionExpression(triggerNode, "", collector);

        // if 块内的条件都被提取
        Assert.That(collector, Has.Count.GreaterThanOrEqualTo(1));
    }
}
