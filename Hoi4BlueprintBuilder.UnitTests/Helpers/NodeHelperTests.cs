using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.UnitTests.Helpers;

[TestFixture]
public class NodeHelperTests
{
    [Test]
    public void SyncNodeChildren_ShouldRemoveNodes_WhenInRemovedList()
    {
        // Arrange
        var text =
            @"
focus_tree = {
    focus = { id = f1 }
    focus = { id = f2 }
}
";

        TextParser.TryParse(string.Empty, text, out var root, out var error);
        var focusTree = root.Nodes.First(); // focus_tree

        var children = focusTree.AllArray.ToList();
        var child1 = children[0]; // f1
        var child2 = children[1]; // f2

        child1.TryGetNode(out var node1);
        child2.TryGetNode(out var node2);

        // Use the actual node instance which has the correct position
        var removedList = new List<Node> { node1 };
        var editorMap = new Dictionary<string, FocusNode>();

        // Act
        NodeHelper.SyncNodeChildren(focusTree, removedList, editorMap, FocusType.Normal);

        // Assert
        Assert.That(focusTree.AllArray.Length, Is.EqualTo(1));

        focusTree.AllArray[0].TryGetNode(out var remainingNode);
        Assert.That(remainingNode, Is.EqualTo(node2));
    }

    [Test]
    public void SyncNodeChildren_ShouldAddNodes_WhenInEditorMap()
    {
        // Arrange
        var root = new Node("focus_tree") { AllArray = [] };
        var removedList = new List<Node>();

        var newFocus = new FocusNode("path", FocusType.Normal)
        {
            Id = "new_focus",
            RawPosition = new FocusPoint(5, 5),
            Icon = "GFX_test",
            Cost = 10
        };
        var editorMap = new Dictionary<string, FocusNode> { { newFocus.Id, newFocus } };

        // Act
        NodeHelper.SyncNodeChildren(root, removedList, editorMap, FocusType.Normal);

        // Assert
        Assert.That(root.AllArray, Has.Length.EqualTo(1));

        var addedChild = root.AllArray[0];
        var isNode = addedChild.TryGetNode(out var addedNode);
        Assert.That(isNode, Is.True);

        var idLeaf = addedNode.Leaves.FirstOrDefault(l => l.Key == "id");
        Assert.That(idLeaf.ValueText, Is.EqualTo("new_focus"));

        Assert.That(editorMap, Is.Empty);
    }

    [Test]
    public void SyncNodeChildren_ShouldIgnoreNodes_WithDifferentType()
    {
        // Arrange
        var root = new Node("focus_tree") { AllArray = Array.Empty<Child>() };
        var removedList = new List<Node>();

        var sharedFocus = new FocusNode("path", FocusType.Shared) { Id = "shared_focus" };
        var editorMap = new Dictionary<string, FocusNode> { { sharedFocus.Id, sharedFocus } };

        // Act
        NodeHelper.SyncNodeChildren(root, removedList, editorMap, FocusType.Normal);

        // Assert
        Assert.That(root.AllArray, Is.Empty);
        Assert.That(editorMap, Is.Not.Empty);
    }

    [Test]
    public void SyncNodeContent_ShouldUpdateLeaves()
    {
        // Arrange
        var focusNode = new Node("focus")
        {
            AllArray =
            [
                ChildHelper.Leaf("cost", 5),
                ChildHelper.Leaf("x", 0),
                ChildHelper.Leaf("y", 0),
                ChildHelper.LeafString("icon", "old_icon")
            ]
        };

        var editorModel = new FocusNode("path", FocusType.Normal)
        {
            Id = "test_focus",
            Cost = 10,
            RawPosition = new FocusPoint(10, 20),
            Icon = "new_icon"
        };

        // Act
        NodeHelper.SyncNodeContent(focusNode, editorModel);

        // Assert
        var leaves = focusNode.Leaves.ToDictionary(l => l.Key, l => l.ValueText);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(leaves["cost"], Is.EqualTo("10"));
            Assert.That(leaves["x"], Is.EqualTo("10"));
            Assert.That(leaves["y"], Is.EqualTo("20"));
            Assert.That(leaves["icon"], Is.EqualTo("new_icon"));
        }
    }

    [Test]
    public void SyncNodeContent_ShouldHandleMutuallyExclusive()
    {
        // Arrange
        var focusNode = new Node("focus") { AllArray = Array.Empty<Child>() };
        var editorModel = new FocusNode("path", FocusType.Normal) { Id = "test_focus" };
        editorModel.AddMutuallyExclusive(new FocusNode("path", FocusType.Normal) { Id = "ex1" });
        editorModel.AddMutuallyExclusive(new FocusNode("path", FocusType.Normal) { Id = "ex2" });

        // Act
        NodeHelper.SyncNodeContent(focusNode, editorModel);

        // Assert
        var exclusiveNode = focusNode.Nodes.FirstOrDefault(n => n.Key == "mutually_exclusive");
        Assert.That(exclusiveNode, Is.Not.Null);
        Assert.That(exclusiveNode.Leaves.Count(), Is.EqualTo(2));
        Assert.That(exclusiveNode.Leaves.Any(l => l.ValueText == "ex1"), Is.True);
        Assert.That(exclusiveNode.Leaves.Any(l => l.ValueText == "ex2"), Is.True);
    }

    [Test]
    public void SyncNodeContent_ShouldHandlePrerequisite()
    {
        // Arrange
        var focusNode = new Node("focus") { AllArray = [] };
        var editorModel = new FocusNode("path", FocusType.Normal) { Id = "test_focus" };

        editorModel.AddPrerequisite([new FocusNode("path", FocusType.Normal) { Id = "pre1" }]);
        editorModel.AddPrerequisite(
            new List<FocusNode>
            {
                new FocusNode("path", FocusType.Normal) { Id = "pre2" },
                new FocusNode("path", FocusType.Normal) { Id = "pre3" }
            }
        );

        // Act
        NodeHelper.SyncNodeContent(focusNode, editorModel);

        // Assert
        var prereqNodes = focusNode.Nodes.Where(n => n.Key == "prerequisite").ToList();
        Assert.That(prereqNodes.Count, Is.EqualTo(2));

        var firstPrereq = prereqNodes.FirstOrDefault(n => n.Leaves.Any(l => l.ValueText == "pre1"));
        Assert.That(firstPrereq, Is.Not.Null);

        var secondPrereq = prereqNodes.FirstOrDefault(n => n.Leaves.Any(l => l.ValueText == "pre2"));
        Assert.That(secondPrereq, Is.Not.Null);
        Assert.That(secondPrereq.Leaves.Any(l => l.ValueText == "pre3"), Is.True);
    }

    [Test]
    public void SyncNodeContent_ShouldHandleRelativePosition()
    {
        // Arrange
        var focusNode = new Node("focus") { AllArray = [] };
        var editorModel = new FocusNode("path", FocusType.Normal) { Id = "test_focus" };
        editorModel.RelativePosition = new FocusNode("path", FocusType.Normal) { Id = "parent_focus" };

        // Act
        NodeHelper.SyncNodeContent(focusNode, editorModel);

        // Assert
        var relLeaf = focusNode.Leaves.FirstOrDefault(l => l.Key == "relative_position_id");
        Assert.That(relLeaf, Is.Not.Null);
        Assert.That(relLeaf.ValueText, Is.EqualTo("parent_focus"));
    }

    [Test]
    public void SyncNodeContent_ShouldHandleCompletionReward()
    {
        // Arrange
        var focusNode = new Node("focus") { AllArray = [] };
        var editorModel = new FocusNode("path", FocusType.Normal)
        {
            Id = "test_focus",
            CompletionReward = "add_political_power = 100"
        };

        // Act
        NodeHelper.SyncNodeContent(focusNode, editorModel);

        // Assert
        var rewardNode = focusNode.Nodes.FirstOrDefault(n => n.Key == "completion_reward");
        Assert.That(rewardNode, Is.Not.Null);
        Assert.That(rewardNode.Leaves.Any(l => l.Key == "add_political_power" && l.ValueText == "100"), Is.True);
    }

    [Test]
    public void AddCompletionRewardToChildrenIfExist_ShouldNotAddChildren_WhenParsingFails()
    {
        // Arrange
        var children = new List<Child>();
        var editorModel = new FocusNode("path", FocusType.Normal)
        {
            CompletionReward = "invalid { syntax"
        };

        // Act
        NodeHelper.AddCompletionRewardToChildrenIfExist(children, editorModel);

        // Assert - children should remain empty when parsing fails
        Assert.That(children, Is.Empty);
    }
}
