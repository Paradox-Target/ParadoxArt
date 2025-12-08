using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.UnitTests;

[TestFixture(TestOf = typeof(FocusNode))]
public sealed class FocusNodeTests
{
    [Test]
    public void CalculatedPosition_WithoutRelative_IsRawPosition()
    {
        var node = new FocusNode("path", default) { RawPosition = new FocusPoint(10, 20) };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.X, Is.EqualTo(10));
            Assert.That(node.Y, Is.EqualTo(20));
        }
    }

    [Test]
    public void CalculatedPosition_WithRelative_AddsOffsets()
    {
        var node = new FocusNode("path", default);
        var relative = new FocusNode("rel", default);

        node.RawPosition = new FocusPoint(5, 6);
        relative.RawPosition = new FocusPoint(3, 4);

        node.RelativePosition = relative;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.X, Is.EqualTo(8)); // 5 + 3
            Assert.That(node.Y, Is.EqualTo(10)); // 6 + 4
        }
    }

    [Test]
    public void SetRawMethods_SubtractRelativeOffset_FromRawPosition()
    {
        var node = new FocusNode("path", default);
        var relative = new FocusNode("rel", default);

        relative.RawPosition = new FocusPoint(2, 3);
        node.RelativePosition = relative;
        node.RawPosition = new FocusPoint(0, 0);

        node.SetRawX(10);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.RawPosition.X, Is.EqualTo(8)); // 10 - 2
            Assert.That(node.RawPosition.Y, Is.Zero);
        }

        node.SetRawY(20);
        Assert.That(node.RawPosition.Y, Is.EqualTo(17)); // 20 - 3

        node.SetRawPosition(30, 40);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.RawPosition.X, Is.EqualTo(28)); // 30 - 2
            Assert.That(node.RawPosition.Y, Is.EqualTo(37)); // 40 - 3
        }
    }

    [Test]
    public void EqualsAndHashCode_EqualWhenIdTypePathRawPositionMatch()
    {
        var a = new FocusNode("p", default);
        var b = new FocusNode("p", default);

        a.Id = "same";
        b.Id = "same";
        a.RawPosition = new FocusPoint(1, 2);
        b.RawPosition = new FocusPoint(1, 2);

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void Dispose_UnsubscribesFromRelativePropertyChanged()
    {
        var node = new FocusNode("p", default);
        var rel = new FocusNode("rel", default);

        node.RelativePosition = rel;

        int count = 0;
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(FocusNode.X) or nameof(FocusNode.Y))
            {
                count++;
            }
        };

        // 触发一次相对位置变更，这应该会传播
        rel.SetRawX(10);
        Assert.That(count, Is.GreaterThanOrEqualTo(1));

        int beforeDispose = count;

        node.Dispose();

        // Dispose 后，相对位置的变更不应再传播
        rel.SetRawX(20);
        Assert.That(count, Is.EqualTo(beforeDispose));
    }

    [Test]
    public void AddPrerequisite_AddsToCollectionAndUpdatesChildren()
    {
        var node = new FocusNode("path", default) { Id = "node" };
        var pre1 = new FocusNode("path", default) { Id = "pre1" };
        var pre2 = new FocusNode("path", default) { Id = "pre2" };

        node.AddPrerequisite([pre1, pre2]);

        Assert.That(node.Prerequisite, Has.Count.EqualTo(1));
        Assert.That(node.Prerequisite[0], Contains.Item(pre1));
        Assert.That(node.Prerequisite[0], Contains.Item(pre2));

        Assert.That(pre1.Children, Contains.Item(node));
        Assert.That(pre2.Children, Contains.Item(node));
    }

    [Test]
    public void RemovePrerequisite_RemovesFromCollectionAndUpdatesChildren()
    {
        var node = new FocusNode("path", default) { Id = "node" };
        var pre1 = new FocusNode("path", default) { Id = "pre1" };
        var pre2 = new FocusNode("path", default) { Id = "pre2" };

        node.AddPrerequisite([pre1, pre2]);
        node.RemovePrerequisite(pre1);

        Assert.That(node.Prerequisite, Has.Count.EqualTo(1));
        Assert.That(node.Prerequisite[0], Does.Not.Contain(pre1));
        Assert.That(node.Prerequisite[0], Contains.Item(pre2));

        Assert.That(pre1.Children, Does.Not.Contain(node));
        Assert.That(pre2.Children, Contains.Item(node));
    }

    [Test]
    public void RemovePrerequisite_RemovesGroupWhenEmpty()
    {
        var node = new FocusNode("path", default) { Id = "node" };
        var pre1 = new FocusNode("path", default) { Id = "pre1" };

        node.AddPrerequisite([pre1]);
        node.RemovePrerequisite(pre1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Prerequisite, Is.Empty);
            Assert.That(pre1.Children, Does.Not.Contain(node));
        }
    }

    [Test]
    public void ClearPrerequisites_RemovesAllAndUpdatesChildren()
    {
        var node = new FocusNode("path", default) { Id = "node" };
        var pre1 = new FocusNode("path", default) { Id = "pre1" };
        var pre2 = new FocusNode("path", default) { Id = "pre2" };

        node.AddPrerequisite([pre1]);
        node.AddPrerequisite([pre2]);

        node.ClearPrerequisites();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Prerequisite, Is.Empty);
            Assert.That(pre1.Children, Does.Not.Contain(node));
            Assert.That(pre2.Children, Does.Not.Contain(node));
        }
    }

    [Test]
    public void ClearChildren_RemovesFromChildrenAndUpdatesPrerequisites()
    {
        var parent = new FocusNode("path", default) { Id = "parent" };
        var parent2 = new FocusNode("path", default) { Id = "parent2" };
        var child1 = new FocusNode("path", default) { Id = "child1" };
        var child2 = new FocusNode("path", default) { Id = "child2" };

        child1.AddPrerequisite([parent]);
        child2.AddPrerequisite([parent]);
        child2.AddPrerequisite([parent2]);

        Assert.That(parent.Children, Contains.Item(child1));
        Assert.That(parent.Children, Contains.Item(child2));
        Assert.That(parent2.Children, Contains.Item(child2));

        parent.ClearChildren();

        Assert.That(parent.Children, Is.Empty);
        Assert.That(child1.Prerequisite, Is.Empty);
        Assert.That(child2.Prerequisite, Is.Not.Empty);
        Assert.That(child2.Prerequisite[0], Is.EquivalentTo([parent2]));
    }

    [Test]
    public void RelativePosition_UpdatesRelativePositionChildren()
    {
        var parent = new FocusNode("path", default) { Id = "parent" };
        var child = new FocusNode("path", default) { Id = "child" };
        var child2 = new FocusNode("path", default) { Id = "child2" };

        child.RelativePosition = parent;
        child2.RelativePosition = parent;

        Assert.That(parent.RelativePositionChildren, Contains.Item(child));
        Assert.That(parent.RelativePositionChildren, Contains.Item(child2));

        child.RelativePosition = null;

        Assert.That(parent.RelativePositionChildren, Does.Not.Contain(child));
        Assert.That(parent.RelativePositionChildren, Contains.Item(child2));
    }

    [Test]
    public void ClearRelativePositionChildren_ResetsChildrenPosition()
    {
        var parent = new FocusNode("path", default) { Id = "parent" };
        parent.RawPosition = new FocusPoint(10, 10);

        var child1 = new FocusNode("path", default) { Id = "child1" };
        child1.RawPosition = new FocusPoint(2, 3); // Relative offset
        child1.RelativePosition = parent;

        var child2 = new FocusNode("path", default) { Id = "child2" };
        child2.RawPosition = new FocusPoint(-1, -2); // Relative offset
        child2.RelativePosition = parent;

        // Verify initial absolute positions
        Assert.That(child1.X, Is.EqualTo(12));
        Assert.That(child1.Y, Is.EqualTo(13));
        Assert.That(child2.X, Is.EqualTo(9));
        Assert.That(child2.Y, Is.EqualTo(8));

        Assert.That(parent.RelativePositionChildren, Has.Count.EqualTo(2));

        parent.ClearRelativePositionChildren();

        Assert.That(parent.RelativePositionChildren, Is.Empty);
        Assert.That(child1.RelativePosition, Is.Null);
        Assert.That(child2.RelativePosition, Is.Null);

        // Verify positions are preserved (converted to absolute)
        Assert.That(child1.X, Is.EqualTo(12));
        Assert.That(child1.Y, Is.EqualTo(13));
        Assert.That(child1.RawPosition.X, Is.EqualTo(12));
        Assert.That(child1.RawPosition.Y, Is.EqualTo(13));

        Assert.That(child2.X, Is.EqualTo(9));
        Assert.That(child2.Y, Is.EqualTo(8));
        Assert.That(child2.RawPosition.X, Is.EqualTo(9));
        Assert.That(child2.RawPosition.Y, Is.EqualTo(8));
    }

    [Test]
    public void ClearMutuallyExclusive_RemovesBidirectionalLinks()
    {
        var node = new FocusNode("path", default) { Id = "node" };
        var other1 = new FocusNode("path", default) { Id = "other1" };
        var other2 = new FocusNode("path", default) { Id = "other2" };

        // Setup bidirectional links
        node.AddMutuallyExclusive(other1);
        node.AddMutuallyExclusive(other2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.MutuallyExclusive, Is.EquivalentTo([other1, other2]));
            Assert.That(other1.MutuallyExclusive, Contains.Item(node));
            Assert.That(other2.MutuallyExclusive, Contains.Item(node));
        }

        node.ClearMutuallyExclusive();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.MutuallyExclusive, Is.Empty);
            Assert.That(other1.MutuallyExclusive, Does.Not.Contain(node));
            Assert.That(other2.MutuallyExclusive, Does.Not.Contain(node));
        }
    }

    [Test]
    public void ConvertToAbsolutePosition()
    {
        var parent = new FocusNode("path", default) { Id = "parent" };
        parent.RawPosition = new FocusPoint(15, 25);
        var child = new FocusNode("path", default) { Id = "child" };
        // Relative offset
        child.RawPosition = new FocusPoint(5, 10);
        child.RelativePosition = parent;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(child.X, Is.EqualTo(20));
            Assert.That(child.Y, Is.EqualTo(35));
        }

        child.ConvertToAbsolutePosition();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(child.RelativePosition, Is.Null);
            Assert.That(child.RawPosition.X, Is.EqualTo(20));
            Assert.That(child.RawPosition.Y, Is.EqualTo(35));
        }
    }

    [Test]
    public void ConvertToRelativePosition_PreventsSelfReference()
    {
        var node = new FocusNode("path", default) { Id = "node" };
        node.RawPosition = new FocusPoint(10, 10);

        bool result = node.ConvertToRelativePosition(node);

        Assert.That(result, Is.False);
        Assert.That(node.RelativePosition, Is.Null);
    }

    [Test]
    public void ConvertToRelativePosition_PreventsDirectCircularReference()
    {
        var nodeA = new FocusNode("path", default) { Id = "A" };
        var nodeB = new FocusNode("path", default) { Id = "B" };

        nodeA.RawPosition = new FocusPoint(0, 0);
        nodeB.RawPosition = new FocusPoint(10, 10);

        // A's position is relative to B
        bool result1 = nodeA.ConvertToRelativePosition(nodeB);
        Assert.That(result1, Is.True);
        Assert.That(nodeA.RelativePosition, Is.EqualTo(nodeB));

        // Try to make B's position relative to A (would create a cycle)
        bool result2 = nodeB.ConvertToRelativePosition(nodeA);
        Assert.That(result2, Is.False);
        Assert.That(nodeB.RelativePosition, Is.Null);
    }

    [Test]
    public void ConvertToRelativePosition_PreventsIndirectCircularReference()
    {
        var nodeA = new FocusNode("path", default) { Id = "A" };
        var nodeB = new FocusNode("path", default) { Id = "B" };
        var nodeC = new FocusNode("path", default) { Id = "C" };

        nodeA.RawPosition = new FocusPoint(0, 0);
        nodeB.RawPosition = new FocusPoint(10, 10);
        nodeC.RawPosition = new FocusPoint(20, 20);

        // A -> B -> C
        bool result1 = nodeA.ConvertToRelativePosition(nodeB);
        Assert.That(result1, Is.True);

        bool result2 = nodeB.ConvertToRelativePosition(nodeC);
        Assert.That(result2, Is.True);

        // 试着让C相对于A（会形成一个循环：A->B->C->A）
        bool result3 = nodeC.ConvertToRelativePosition(nodeA);
        Assert.That(result3, Is.False);
        Assert.That(nodeC.RelativePosition, Is.Null);
    }

    [Test]
    public void ConvertToRelativePosition_PreventsFourNodeCircularReference()
    {
        var nodeA = new FocusNode("path", default) { Id = "A" };
        var nodeB = new FocusNode("path", default) { Id = "B" };
        var nodeC = new FocusNode("path", default) { Id = "C" };
        var nodeD = new FocusNode("path", default) { Id = "D" };

        nodeA.RawPosition = new FocusPoint(0, 0);
        nodeB.RawPosition = new FocusPoint(10, 10);
        nodeC.RawPosition = new FocusPoint(20, 20);
        nodeD.RawPosition = new FocusPoint(30, 30);

        // 建立相对位置链：A -> B -> C -> D
        Assert.That(nodeA.ConvertToRelativePosition(nodeB), Is.True);
        Assert.That(nodeB.ConvertToRelativePosition(nodeC), Is.True);
        Assert.That(nodeC.ConvertToRelativePosition(nodeD), Is.True);

        // 试着让 D 相对于 A（会形成循环）
        Assert.That(nodeD.ConvertToRelativePosition(nodeA), Is.False);
        Assert.That(nodeD.RelativePosition, Is.Null);

        // 试着让 D 相对于 B（会形成循环）
        Assert.That(nodeD.ConvertToRelativePosition(nodeB), Is.False);
        Assert.That(nodeD.RelativePosition, Is.Null);

        // 试着让 D 相对于 C（会形成循环）
        Assert.That(nodeD.ConvertToRelativePosition(nodeC), Is.False);
        Assert.That(nodeD.RelativePosition, Is.Null);
    }

    [Test]
    public void ConvertToRelativePosition_AllowsNonCircularReferences()
    {
        var nodeA = new FocusNode("path", default) { Id = "A" };
        var nodeB = new FocusNode("path", default) { Id = "B" };
        var nodeC = new FocusNode("path", default) { Id = "C" };
        var nodeD = new FocusNode("path", default) { Id = "D" };

        nodeA.RawPosition = new FocusPoint(0, 0);
        nodeB.RawPosition = new FocusPoint(10, 10);
        nodeC.RawPosition = new FocusPoint(20, 20);
        nodeD.RawPosition = new FocusPoint(30, 30);

        // Create chain: A -> B
        Assert.That(nodeA.ConvertToRelativePosition(nodeB), Is.True);

        // C and D can both reference B without creating a cycle
        Assert.That(nodeC.ConvertToRelativePosition(nodeB), Is.True);
        Assert.That(nodeD.ConvertToRelativePosition(nodeB), Is.True);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nodeA.RelativePosition, Is.EqualTo(nodeB));
            Assert.That(nodeC.RelativePosition, Is.EqualTo(nodeB));
            Assert.That(nodeD.RelativePosition, Is.EqualTo(nodeB));
        }
    }

    [Test]
    public void ConvertToRelativePosition_CalculatesCorrectOffsets()
    {
        var parent = new FocusNode("path", default) { Id = "parent" };
        var child = new FocusNode("path", default) { Id = "child" };

        parent.RawPosition = new FocusPoint(15, 25);
        child.RawPosition = new FocusPoint(20, 35);

        bool result = child.ConvertToRelativePosition(parent);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(child.RelativePosition, Is.EqualTo(parent));
            Assert.That(child.RawPosition.X, Is.EqualTo(5)); // 20 - 15
            Assert.That(child.RawPosition.Y, Is.EqualTo(10)); // 35 - 25
            Assert.That(child.X, Is.EqualTo(20)); // Absolute position preserved
            Assert.That(child.Y, Is.EqualTo(35)); // Absolute position preserved
        }
    }

    [Test]
    public void ConvertToRelativePosition_CanSwitchRelativeParent()
    {
        var nodeA = new FocusNode("path", default) { Id = "A" };
        var nodeB = new FocusNode("path", default) { Id = "B" };
        var nodeC = new FocusNode("path", default) { Id = "C" };

        nodeA.RawPosition = new FocusPoint(0, 0);
        nodeB.RawPosition = new FocusPoint(10, 10);
        nodeC.RawPosition = new FocusPoint(25, 25);

        // C relative to A
        Assert.That(nodeC.ConvertToRelativePosition(nodeA), Is.True);
        Assert.That(nodeC.X, Is.EqualTo(25));
        Assert.That(nodeC.Y, Is.EqualTo(25));

        // Switch C to be relative to B instead
        Assert.That(nodeC.ConvertToRelativePosition(nodeB), Is.True);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(nodeC.RelativePosition, Is.EqualTo(nodeB));
            Assert.That(nodeC.X, Is.EqualTo(25)); // Position should be preserved
            Assert.That(nodeC.Y, Is.EqualTo(25)); // Position should be preserved
            Assert.That(nodeC.RawPosition.X, Is.EqualTo(15)); // 25 - 10
            Assert.That(nodeC.RawPosition.Y, Is.EqualTo(15)); // 25 - 10
        }
    }
}
