using System.Globalization;
using Avalonia.Controls;
using Desktop.Converters;

namespace Desktop.UITests.Converters;

[TestFixture]
public class BoolToGridLengthConverterTests
{
    private BoolToGridLengthConverter _converter = null!;

    [SetUp]
    public void Setup()
    {
        _converter = new BoolToGridLengthConverter();
    }

    [Test]
    public void Instance_Should_Return_Same_Instance()
    {
        // Act
        var instance1 = BoolToGridLengthConverter.Instance;
        var instance2 = BoolToGridLengthConverter.Instance;

        // Assert
        Assert.That(instance1, Is.SameAs(instance2));
    }

    [Test]
    public void Convert_WithTrue_Should_Return_StarGridLength()
    {
        // Act
        var result = _converter.Convert(true, typeof(GridLength), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<GridLength>());

            var gridLength = (GridLength)result!;
            Assert.That(gridLength.Value, Is.EqualTo(1.0));
            Assert.That(gridLength.GridUnitType, Is.EqualTo(GridUnitType.Star));
            Assert.That(gridLength.IsStar, Is.True);
            Assert.That(gridLength.IsAbsolute, Is.False);
            Assert.That(gridLength.IsAuto, Is.False);
        });
    }

    [Test]
    public void Convert_WithFalse_Should_Return_ZeroGridLength()
    {
        // Act
        var result = _converter.Convert(false, typeof(GridLength), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<GridLength>());

            var gridLength = (GridLength)result!;
            Assert.That(gridLength.Value, Is.EqualTo(0.0));
            Assert.That(gridLength.GridUnitType, Is.EqualTo(GridUnitType.Pixel));
            Assert.That(gridLength.IsStar, Is.False);
            Assert.That(gridLength.IsAbsolute, Is.True);
            Assert.That(gridLength.IsAuto, Is.False);
        });
    }

    [Test]
    public void Convert_WithNull_Should_Return_ZeroGridLength()
    {
        // Act
        var result = _converter.Convert(null, typeof(GridLength), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<GridLength>());

            var gridLength = (GridLength)result!;
            Assert.That(gridLength.Value, Is.EqualTo(0.0));
            Assert.That(gridLength.GridUnitType, Is.EqualTo(GridUnitType.Pixel));
        });
    }

    [Test]
    public void Convert_WithNonBoolValue_Should_Return_ZeroGridLength()
    {
        // Arrange
        var nonBoolValues = new object[] { "true", 1, 0, "false", "some string", 45.67 };

        foreach (var value in nonBoolValues)
        {
            // Act
            var result = _converter.Convert(value, typeof(GridLength), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for value: {value}");
                Assert.That(result, Is.TypeOf<GridLength>(), $"Failed for value: {value}");

                var gridLength = (GridLength)result!;
                Assert.That(gridLength.Value, Is.EqualTo(0.0), $"Failed for value: {value}");
                Assert.That(gridLength.GridUnitType, Is.EqualTo(GridUnitType.Pixel), $"Failed for value: {value}");
            });
        }
    }

    [Test]
    public void Convert_WithDifferentTargetTypes_Should_Still_Work()
    {
        // Act & Assert
        var result1 = _converter.Convert(true, typeof(object), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert(true, typeof(GridLength), null, CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.TypeOf<GridLength>());
            Assert.That(result2, Is.TypeOf<GridLength>());

            var gridLength1 = (GridLength)result1!;
            var gridLength2 = (GridLength)result2!;
            Assert.That(gridLength1.Value, Is.EqualTo(gridLength2.Value));
            Assert.That(gridLength1.GridUnitType, Is.EqualTo(gridLength2.GridUnitType));
        });
    }

    [Test]
    public void Convert_WithDifferentParameters_Should_IgnoreParameter()
    {
        // Act
        var result1 = _converter.Convert(true, typeof(GridLength), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert(true, typeof(GridLength), "some parameter", CultureInfo.InvariantCulture);

        // Assert
        var gridLength1 = (GridLength)result1!;
        var gridLength2 = (GridLength)result2!;

        Assert.Multiple(() =>
        {
            Assert.That(gridLength1.Value, Is.EqualTo(gridLength2.Value));
            Assert.That(gridLength1.GridUnitType, Is.EqualTo(gridLength2.GridUnitType));
        });
    }

    [Test]
    public void Convert_WithDifferentCultures_Should_IgnoreCulture()
    {
        // Act
        var result1 = _converter.Convert(true, typeof(GridLength), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert(true, typeof(GridLength), null, new CultureInfo("en-US"));
        var result3 = _converter.Convert(true, typeof(GridLength), null, new CultureInfo("fi-FI"));

        // Assert
        var gridLength1 = (GridLength)result1!;
        var gridLength2 = (GridLength)result2!;
        var gridLength3 = (GridLength)result3!;

        Assert.Multiple(() =>
        {
            Assert.That(gridLength1.Value, Is.EqualTo(gridLength2.Value));
            Assert.That(gridLength1.GridUnitType, Is.EqualTo(gridLength2.GridUnitType));
            Assert.That(gridLength1.Value, Is.EqualTo(gridLength3.Value));
            Assert.That(gridLength1.GridUnitType, Is.EqualTo(gridLength3.GridUnitType));
        });
    }

    [Test]
    public void ConvertBack_Should_Throw_NotImplementedException()
    {
        // Act & Assert
        Assert.Throws<System.NotImplementedException>(() =>
            _converter.ConvertBack(new GridLength(1, GridUnitType.Star), typeof(bool), null, CultureInfo.InvariantCulture));
    }

    [Test]
    public void Convert_GridLength_Properties_Should_Be_Correct()
    {
        // Act
        var starResult = _converter.Convert(true, typeof(GridLength), null, CultureInfo.InvariantCulture);
        var zeroResult = _converter.Convert(false, typeof(GridLength), null, CultureInfo.InvariantCulture);

        // Assert
        var starLength = (GridLength)starResult!;
        var zeroLength = (GridLength)zeroResult!;

        Assert.Multiple(() =>
        {
            // Star length properties
            Assert.That(starLength.Value, Is.EqualTo(1.0));
            Assert.That(starLength.IsStar, Is.True);
            Assert.That(starLength.IsAbsolute, Is.False);
            Assert.That(starLength.IsAuto, Is.False);

            // Zero length properties  
            Assert.That(zeroLength.Value, Is.EqualTo(0.0));
            Assert.That(zeroLength.IsStar, Is.False);
            Assert.That(zeroLength.IsAbsolute, Is.True);
            Assert.That(zeroLength.IsAuto, Is.False);
        });
    }

    [Test]
    public void Convert_Multiple_Calls_Should_Return_Equal_Values()
    {
        // Act
        var result1 = _converter.Convert(true, typeof(GridLength), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert(true, typeof(GridLength), null, CultureInfo.InvariantCulture);
        var result3 = _converter.Convert(false, typeof(GridLength), null, CultureInfo.InvariantCulture);
        var result4 = _converter.Convert(false, typeof(GridLength), null, CultureInfo.InvariantCulture);

        // Assert
        var gridLength1 = (GridLength)result1!;
        var gridLength2 = (GridLength)result2!;
        var gridLength3 = (GridLength)result3!;
        var gridLength4 = (GridLength)result4!;

        Assert.Multiple(() =>
        {
            Assert.That(gridLength1, Is.EqualTo(gridLength2));
            Assert.That(gridLength3, Is.EqualTo(gridLength4));
            Assert.That(gridLength1, Is.Not.EqualTo(gridLength3));
        });
    }

    [TestCase(true, 1.0, GridUnitType.Star)]
    [TestCase(false, 0.0, GridUnitType.Pixel)]
    public void Convert_WithTestCases_Should_Return_Expected_Values(bool input, double expectedValue, GridUnitType expectedType)
    {
        // Act
        var result = _converter.Convert(input, typeof(GridLength), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<GridLength>());
            var gridLength = (GridLength)result!;
            Assert.That(gridLength.Value, Is.EqualTo(expectedValue));
            Assert.That(gridLength.GridUnitType, Is.EqualTo(expectedType));
        });
    }
}