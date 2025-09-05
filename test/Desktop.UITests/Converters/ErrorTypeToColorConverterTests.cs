using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Desktop.Converters;

namespace Desktop.UITests.Converters;

[TestFixture]
public class ErrorTypeToColorConverterTests
{
    private ErrorTypeToColorConverter _converter = null!;

    [SetUp]
    public void Setup()
    {
        _converter = new ErrorTypeToColorConverter();
    }

    [Test]
    public void Instance_Should_Return_Same_Instance()
    {
        // Act
        var instance1 = ErrorTypeToColorConverter.Instance;
        var instance2 = ErrorTypeToColorConverter.Instance;

        // Assert
        Assert.That(instance1, Is.SameAs(instance2));
    }

    [Test]
    public void Convert_WithError_Should_Return_RedColor()
    {
        // Act
        var result = _converter.Convert("error", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<ImmutableSolidColorBrush>());

            var brush = result as ImmutableSolidColorBrush;
            var expectedColor = Color.Parse("#FF6B6B");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor));
        });
    }

    [Test]
    public void Convert_WithWarning_Should_Return_YellowColor()
    {
        // Act
        var result = _converter.Convert("warning", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<ImmutableSolidColorBrush>());

            var brush = result as ImmutableSolidColorBrush;
            var expectedColor = Color.Parse("#FFD93D");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor));
        });
    }

    [Test]
    public void Convert_WithUnknownErrorType_Should_Return_DefaultColor()
    {
        // Arrange
        var unknownTypes = new[] { "info", "debug", "trace", "custom", "unknown" };

        foreach (var errorType in unknownTypes)
        {
            // Act
            var result = _converter.Convert(errorType, typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for error type: {errorType}");
                Assert.That(result, Is.TypeOf<ImmutableSolidColorBrush>(), $"Failed for error type: {errorType}");

                var brush = result as ImmutableSolidColorBrush;
                var expectedColor = Color.Parse("#CCCCCC");
                Assert.That(brush!.Color, Is.EqualTo(expectedColor), $"Failed for error type: {errorType}");
            });
        }
    }

    [Test]
    public void Convert_WithCaseVariations_Should_Work()
    {
        // Arrange
        var errorVariations = new[] { "ERROR", "Error", "eRrOr", "error" };
        var warningVariations = new[] { "WARNING", "Warning", "wArNiNg", "warning" };

        // Act & Assert for error variations
        foreach (var errorType in errorVariations)
        {
            var result = _converter.Convert(errorType, typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);
            var brush = result as ImmutableSolidColorBrush;
            var expectedColor = Color.Parse("#FF6B6B");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor), $"Failed for error type: {errorType}");
        }

        // Act & Assert for warning variations
        foreach (var warningType in warningVariations)
        {
            var result = _converter.Convert(warningType, typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);
            var brush = result as ImmutableSolidColorBrush;
            var expectedColor = Color.Parse("#FFD93D");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor), $"Failed for warning type: {warningType}");
        }
    }

    [Test]
    public void Convert_WithNull_Should_Return_DefaultColor()
    {
        // Act
        var result = _converter.Convert(null, typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<ImmutableSolidColorBrush>());

            var brush = result as ImmutableSolidColorBrush;
            var expectedColor = Color.Parse("#CCCCCC");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor));
        });
    }

    [Test]
    public void Convert_WithNonStringValue_Should_Return_DefaultColor()
    {
        // Arrange
        var nonStringValues = new object[] { 123, true, false, 45.67, new object() };

        foreach (var value in nonStringValues)
        {
            // Act
            var result = _converter.Convert(value, typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for value: {value}");
                Assert.That(result, Is.TypeOf<ImmutableSolidColorBrush>(), $"Failed for value: {value}");

                var brush = result as ImmutableSolidColorBrush;
                var expectedColor = Color.Parse("#CCCCCC");
                Assert.That(brush!.Color, Is.EqualTo(expectedColor), $"Failed for value: {value}");
            });
        }
    }

    [Test]
    public void Convert_WithEmptyString_Should_Return_DefaultColor()
    {
        // Act
        var result = _converter.Convert("", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<ImmutableSolidColorBrush>());

            var brush = result as ImmutableSolidColorBrush;
            var expectedColor = Color.Parse("#CCCCCC");
            Assert.That(brush!.Color, Is.EqualTo(expectedColor));
        });
    }

    [Test]
    public void Convert_WithWhitespace_Should_Return_DefaultColor()
    {
        // Arrange
        var whitespaceValues = new[] { " ", "\t", "\n", "   ", "\r\n" };

        foreach (var value in whitespaceValues)
        {
            // Act
            var result = _converter.Convert(value, typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for whitespace: '{value}'");
                Assert.That(result, Is.TypeOf<ImmutableSolidColorBrush>(), $"Failed for whitespace: '{value}'");

                var brush = result as ImmutableSolidColorBrush;
                var expectedColor = Color.Parse("#CCCCCC");
                Assert.That(brush!.Color, Is.EqualTo(expectedColor), $"Failed for whitespace: '{value}'");
            });
        }
    }

    [Test]
    public void Convert_WithDifferentTargetTypes_Should_Still_Work()
    {
        // Act & Assert
        var result1 = _converter.Convert("error", typeof(object), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert("error", typeof(Brush), null, CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.TypeOf<ImmutableSolidColorBrush>());
            Assert.That(result2, Is.TypeOf<ImmutableSolidColorBrush>());
        });
    }

    [Test]
    public void Convert_WithDifferentParameters_Should_IgnoreParameter()
    {
        // Act
        var result1 = _converter.Convert("error", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert("error", typeof(ImmutableSolidColorBrush), "some parameter", CultureInfo.InvariantCulture);

        // Assert
        var brush1 = result1 as ImmutableSolidColorBrush;
        var brush2 = result2 as ImmutableSolidColorBrush;

        Assert.That(brush1!.Color, Is.EqualTo(brush2!.Color));
    }

    [Test]
    public void Convert_WithDifferentCultures_Should_IgnoreCulture()
    {
        // Act
        var result1 = _converter.Convert("error", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert("error", typeof(ImmutableSolidColorBrush), null, new CultureInfo("en-US"));
        var result3 = _converter.Convert("error", typeof(ImmutableSolidColorBrush), null, new CultureInfo("fi-FI"));

        // Assert
        var brush1 = result1 as ImmutableSolidColorBrush;
        var brush2 = result2 as ImmutableSolidColorBrush;
        var brush3 = result3 as ImmutableSolidColorBrush;

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
            _converter.ConvertBack(new ImmutableSolidColorBrush(Colors.Red), typeof(string), null, CultureInfo.InvariantCulture));
    }

    [Test]
    public void Convert_Colors_Should_Be_Correct_HexValues()
    {
        // Act
        var errorResult = _converter.Convert("error", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);
        var warningResult = _converter.Convert("warning", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);
        var defaultResult = _converter.Convert("unknown", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert
        var errorBrush = errorResult as ImmutableSolidColorBrush;
        var warningBrush = warningResult as ImmutableSolidColorBrush;
        var defaultBrush = defaultResult as ImmutableSolidColorBrush;

        Assert.Multiple(() =>
        {
            // Error color should be red (#FF6B6B)
            Assert.That(errorBrush!.Color.R, Is.EqualTo(0xFF));
            Assert.That(errorBrush.Color.G, Is.EqualTo(0x6B));
            Assert.That(errorBrush.Color.B, Is.EqualTo(0x6B));
            Assert.That(errorBrush.Color.A, Is.EqualTo(0xFF));

            // Warning color should be yellow (#FFD93D)
            Assert.That(warningBrush!.Color.R, Is.EqualTo(0xFF));
            Assert.That(warningBrush.Color.G, Is.EqualTo(0xD9));
            Assert.That(warningBrush.Color.B, Is.EqualTo(0x3D));
            Assert.That(warningBrush.Color.A, Is.EqualTo(0xFF));

            // Default color should be gray (#CCCCCC)
            Assert.That(defaultBrush!.Color.R, Is.EqualTo(0xCC));
            Assert.That(defaultBrush.Color.G, Is.EqualTo(0xCC));
            Assert.That(defaultBrush.Color.B, Is.EqualTo(0xCC));
            Assert.That(defaultBrush.Color.A, Is.EqualTo(0xFF));
        });
    }

    [Test]
    public void Convert_Same_ErrorType_Should_Return_Same_Brush_Instance()
    {
        // Act - Call multiple times with same error type
        var result1 = _converter.Convert("error", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);
        var result2 = _converter.Convert("error", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);
        var result3 = _converter.Convert("ERROR", typeof(ImmutableSolidColorBrush), null, CultureInfo.InvariantCulture);

        // Assert - Should return the same brush instance (optimization verification)
        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.SameAs(result2));
            Assert.That(result1, Is.SameAs(result3));
        });
    }
}