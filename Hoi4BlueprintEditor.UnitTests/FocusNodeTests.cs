using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.UnitTests;

[TestFixture(TestOf = typeof(FocusNode))]
public sealed class FocusNodeTests
{
    [Test]
    public void CalculatedPosition_WithoutRelative_IsRawPosition()
    {
        var node = new FocusNode("path", default) { RawPosition = new FocusPoint(10, 20) };

        Assert.That(node.X, Is.EqualTo(10));
        Assert.That(node.Y, Is.EqualTo(20));
    }

    [Test]
    public void CalculatedPosition_WithRelative_AddsOffsets()
    {
        var node = new FocusNode("path", default);
        var relative = new FocusNode("rel", default);

        node.RawPosition = new FocusPoint(5, 6);
        relative.RawPosition = new FocusPoint(3, 4);

        node.RelativePosition = relative;

        Assert.That(node.X, Is.EqualTo(8)); // 5 + 3
        Assert.That(node.Y, Is.EqualTo(10)); // 6 + 4
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
        Assert.That(node.RawPosition.X, Is.EqualTo(8)); // 10 - 2
        Assert.That(node.RawPosition.Y, Is.Zero);

        node.SetRawY(20);
        Assert.That(node.RawPosition.Y, Is.EqualTo(17)); // 20 - 3

        node.SetRawPosition(30, 40);
        Assert.That(node.RawPosition.X, Is.EqualTo(28)); // 30 - 2
        Assert.That(node.RawPosition.Y, Is.EqualTo(37)); // 40 - 3
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

        Assert.That(node.Prerequisite, Is.Empty);
        Assert.That(pre1.Children, Does.Not.Contain(node));
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
}
