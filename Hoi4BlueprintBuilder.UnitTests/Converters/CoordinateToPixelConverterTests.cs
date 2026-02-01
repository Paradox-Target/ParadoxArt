using System.Globalization;
using Hoi4BlueprintBuilder.Core.Infrastructure.Converters;

namespace Hoi4BlueprintBuilder.UnitTests.Converters;

[TestFixture]
public class CoordinateToPixelConverterTests
{
    private CoordinateToPixelConverter _converter;

    [SetUp]
    public void Setup()
    {
        _converter = new CoordinateToPixelConverter();
    }

    [Test]
    public void Convert_WithValidIntegerAndDoubleParameter_ReturnsMultipliedValue()
    {
        // Arrange
        int coordinate = 5;
        double multiplier = 10.0;
        double expected = 50.0;

        // Act
        var result = _converter.Convert(coordinate, typeof(double), multiplier, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.InstanceOf<double>());
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Convert_WithZeroCoordinate_ReturnsZero()
    {
        // Arrange
        int coordinate = 0;
        double multiplier = 10.0;

        // Act
        var result = _converter.Convert(coordinate, typeof(double), multiplier, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.Zero);
    }

    [Test]
    public void Convert_WithNegativeCoordinate_ReturnsNegativeValue()
    {
        // Arrange
        int coordinate = -5;
        double multiplier = 10.0;
        double expected = -50.0;

        // Act
        var result = _converter.Convert(coordinate, typeof(double), multiplier, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Convert_WithNonIntegerValue_ReturnsZero()
    {
        // Arrange
        string invalidValue = "10"; // Not an int
        double multiplier = 10.0;

        // Act
        var result = _converter.Convert(
            invalidValue,
            typeof(double),
            multiplier,
            CultureInfo.InvariantCulture
        );

        // Assert
        Assert.That(result, Is.Zero);
    }

    [Test]
    public void Convert_WithNullValue_ReturnsZero()
    {
        // Arrange
        object? invalidValue = null;
        double multiplier = 10.0;

        // Act
        var result = _converter.Convert(
            invalidValue,
            typeof(double),
            multiplier,
            CultureInfo.InvariantCulture
        );

        // Assert
        Assert.That(result, Is.Zero);
    }

    [Test]
    public void Convert_WithNonDoubleParameter_ReturnsZero()
    {
        // Arrange
        int coordinate = 10;
        string invalidParameter = "2.0"; // Current implementation expects double, not string parsing

        // Act
        var result = _converter.Convert(
            coordinate,
            typeof(double),
            invalidParameter,
            CultureInfo.InvariantCulture
        );

        // Assert
        Assert.That(result, Is.Zero);
    }

    [Test]
    public void Convert_WithNullParameter_ReturnsZero()
    {
        // Arrange
        int coordinate = 10;
        object? invalidParameter = null;

        // Act
        var result = _converter.Convert(
            coordinate,
            typeof(double),
            invalidParameter,
            CultureInfo.InvariantCulture
        );

        // Assert
        Assert.That(result, Is.Zero);
    }

    [Test]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        object value = 100;

        // Act & Assert
        Assert.Throws<NotSupportedException>(
            () => _converter.ConvertBack(value, typeof(int), null, CultureInfo.InvariantCulture)
        );
    }
}
