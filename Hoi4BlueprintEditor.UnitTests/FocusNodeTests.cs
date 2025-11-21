using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.UnitTests;

[TestFixture(TestOf = typeof(FocusNode))]
public sealed class FocusNodeTests
{
    [Test]
    public void CalculatedPosition_WithoutRelative_IsRawPosition()
    {
        var node = new FocusNode("path", default)
        {
            RawPosition = new FocusPoint(10, 20)
        };

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
}