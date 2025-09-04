using System.Globalization;
using Avalonia.Media;
using Desktop.Converters;

namespace Desktop.UITests.Converters;

[TestFixture]
public class BoolToColorConverterTests
{
    private BoolToColorConverter _converter = null!;

    [SetUp]
    public void Setup()
    {
        _converter = new BoolToColorConverter();
    }

    [Test]
    public void Instance_Should_Return_Same_Instance()
    {
        // Act
        var instance1 = BoolToColorConverter.Instance;
        var instance2 = BoolToColorConverter.Instance;

        // Assert
        Assert.That(instance1, Is.SameAs(instance2));
    }

    [Test]
    public void Convert_WithTrue_Should_Return_ActiveColor()
    {
        // Act
        var result = _converter.Convert(true, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<SolidColorBrush>());
            
            var brush = result as SolidColorBrush;
            var expectedColor = Color.Parse("#007ACC");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor));
        });
    }

    [Test]
    public void Convert_WithFalse_Should_Return_InactiveColor()
    {
        // Act
        var result = _converter.Convert(false, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<SolidColorBrush>());
            
            var brush = result as SolidColorBrush;
            var expectedColor = Color.Parse("#3E3E42");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor));
        });
    }

    [Test]
    public void Convert_WithNull_Should_Return_InactiveColor()
    {
        // Act
        var result = _converter.Convert(null, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<SolidColorBrush>());
            
            var brush = result as SolidColorBrush;
            var expectedColor = Color.Parse("#3E3E42");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor));
        });
    }

    [Test]
    public void Convert_WithNonBoolValue_Should_Return_InactiveColor()
    {
        // Arrange
        var nonBoolValues = new object[] { "true", 1, 0, "false", "some string" };

        foreach (var value in nonBoolValues)
        {
            // Act
            var result = _converter.Convert(value, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for value: {value}");
                Assert.That(result, Is.TypeOf<SolidColorBrush>(), $"Failed for value: {value}");
                
                var brush = result as SolidColorBrush;
                var expectedColor = Color.Parse("#3E3E42");
                Assert.That(brush!.Color, Is.EqualTo(expectedColor), $"Failed for value: {value}");
            });
        }
    }

    [Test]
    public void Convert_WithDifferentTargetTypes_Should_Still_Work()
    {
        // Act & Assert
        var result1 = _converter.Convert(true, typeof(object), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert(true, typeof(Brush), null, CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.TypeOf<SolidColorBrush>());
            Assert.That(result2, Is.TypeOf<SolidColorBrush>());
        });
    }

    [Test]
    public void Convert_WithDifferentParameters_Should_IgnoreParameter()
    {
        // Act
        var result1 = _converter.Convert(true, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert(true, typeof(SolidColorBrush), "some parameter", CultureInfo.InvariantCulture);

        // Assert
        var brush1 = result1 as SolidColorBrush;
        var brush2 = result2 as SolidColorBrush;
        
        Assert.That(brush1!.Color, Is.EqualTo(brush2!.Color));
    }

    [Test]
    public void Convert_WithDifferentCultures_Should_IgnoreCulture()
    {
        // Act
        var result1 = _converter.Convert(true, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert(true, typeof(SolidColorBrush), null, new CultureInfo("en-US"));
        var result3 = _converter.Convert(true, typeof(SolidColorBrush), null, new CultureInfo("fi-FI"));

        // Assert
        var brush1 = result1 as SolidColorBrush;
        var brush2 = result2 as SolidColorBrush;
        var brush3 = result3 as SolidColorBrush;
        
        Assert.Multiple(() =>
        {
            Assert.That(brush1!.Color, Is.EqualTo(brush2!.Color));
            Assert.That(brush1.Color, Is.EqualTo(brush3!.Color));
        });
    }

    [Test]
    public void ConvertBack_Should_Throw_NotImplementedException()
    {
        // Act & Assert
        Assert.Throws<System.NotImplementedException>(() => 
            _converter.ConvertBack(new SolidColorBrush(Colors.Blue), typeof(bool), null, CultureInfo.InvariantCulture));
    }

    [Test]
    public void Convert_Colors_Should_Be_Correct_HexValues()
    {
        // Act
        var activeResult = _converter.Convert(true, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
        var inactiveResult = _converter.Convert(false, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        var activeBrush = activeResult as SolidColorBrush;
        var inactiveBrush = inactiveResult as SolidColorBrush;

        Assert.Multiple(() =>
        {
            // Active color should be bright blue (#007ACC)
            Assert.That(activeBrush!.Color.R, Is.EqualTo(0x00));
            Assert.That(activeBrush.Color.G, Is.EqualTo(0x7A));
            Assert.That(activeBrush.Color.B, Is.EqualTo(0xCC));
            Assert.That(activeBrush.Color.A, Is.EqualTo(0xFF));

            // Inactive color should be dark gray (#3E3E42)
            Assert.That(inactiveBrush!.Color.R, Is.EqualTo(0x3E));
            Assert.That(inactiveBrush.Color.G, Is.EqualTo(0x3E));
            Assert.That(inactiveBrush.Color.B, Is.EqualTo(0x42));
            Assert.That(inactiveBrush.Color.A, Is.EqualTo(0xFF));
        });
    }
}