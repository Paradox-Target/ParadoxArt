using Hoi4BlueprintBuilder.Core.Models.Focus;

namespace Hoi4BlueprintBuilder.UnitTests;

[TestFixture(TestOf = typeof(FocusPoint))]
public sealed class FocusPointTests
{
    [Test]
    public void Constructor_ShouldSetProperties()
    {
        var point = new FocusPoint(10, 20);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(point.X, Is.EqualTo(10));
            Assert.That(point.Y, Is.EqualTo(20));
        }
    }

    [Test]
    public void Equals_ShouldReturnTrueForSameCoordinates()
    {
        var point1 = new FocusPoint(1, 2);
        var point2 = new FocusPoint(1, 2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(point1.Equals(point2), Is.True);
            Assert.That(point1.Equals((object)point2), Is.True);
        }
    }

    [Test]
    public void Equals_ShouldReturnFalseForDifferentCoordinates()
    {
        var point1 = new FocusPoint(1, 2);
        var point2 = new FocusPoint(1, 3);
        var point3 = new FocusPoint(2, 2);

        Assert.That(point1.Equals(point2), Is.False);
        Assert.That(point1.Equals(point3), Is.False);
        Assert.That(point1.Equals(null), Is.False);
        Assert.That(point1.Equals(new object()), Is.False);
    }

    [Test]
    public void GetHashCode_ShouldReturnSameValueForSameCoordinates()
    {
        var point1 = new FocusPoint(100, 200);
        var point2 = new FocusPoint(100, 200);

        Assert.That(point1.GetHashCode(), Is.EqualTo(point2.GetHashCode()));
    }

    [Test]
    public void EqualityOperators_ShouldWorkCorrectly()
    {
        var point1 = new FocusPoint(1, 1);
        var point2 = new FocusPoint(1, 1);
        var point3 = new FocusPoint(2, 2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(point1 == point2, Is.True);
            Assert.That(point1 != point2, Is.False);
            Assert.That(point1 == point3, Is.False);
            Assert.That(point1 != point3, Is.True);
        }
    }
}
