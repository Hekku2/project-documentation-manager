using System.Globalization;
using Desktop.Converters;

namespace Desktop.UITests.Converters;

[TestFixture]
public class CountToBooleanConverterTests
{
    private CountToBooleanConverter _zeroIsTrueConverter = null!;
    private CountToBooleanConverter _zeroIsFalseConverter = null!;

    [SetUp]
    public void Setup()
    {
        _zeroIsTrueConverter = new CountToBooleanConverter { TrueIfZero = true };
        _zeroIsFalseConverter = new CountToBooleanConverter { TrueIfZero = false };
    }

    [Test]
    public void ZeroIsTrue_Static_Instance_Should_Return_Same_Instance()
    {
        // Act
        var instance1 = CountToBooleanConverter.ZeroIsTrue;
        var instance2 = CountToBooleanConverter.ZeroIsTrue;

        // Assert
        Assert.That(instance1, Is.SameAs(instance2));
    }

    [Test]
    public void ZeroIsFalse_Static_Instance_Should_Return_Same_Instance()
    {
        // Act
        var instance1 = CountToBooleanConverter.ZeroIsFalse;
        var instance2 = CountToBooleanConverter.ZeroIsFalse;

        // Assert
        Assert.That(instance1, Is.SameAs(instance2));
    }

    [Test]
    public void Static_Instances_Should_Have_Correct_TrueIfZero_Values()
    {
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(CountToBooleanConverter.ZeroIsTrue.TrueIfZero, Is.True);
            Assert.That(CountToBooleanConverter.ZeroIsFalse.TrueIfZero, Is.False);
        });
    }

    [Test]
    public void ZeroIsTrue_Convert_WithZero_Should_Return_True()
    {
        // Act
        var result = _zeroIsTrueConverter.Convert(0, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<bool>());
            Assert.That((bool)result!, Is.True);
        });
    }

    [Test]
    public void ZeroIsTrue_Convert_WithPositiveCount_Should_Return_False()
    {
        // Arrange
        var positiveCounts = new[] { 1, 2, 5, 10, 100, 999, int.MaxValue };

        foreach (var count in positiveCounts)
        {
            // Act
            var result = _zeroIsTrueConverter.Convert(count, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for count: {count}");
                Assert.That(result, Is.TypeOf<bool>(), $"Failed for count: {count}");
                Assert.That((bool)result!, Is.False, $"Failed for count: {count}");
            });
        }
    }

    [Test]
    public void ZeroIsTrue_Convert_WithNegativeCount_Should_Return_False()
    {
        // Arrange
        var negativeCounts = new[] { -1, -2, -5, -10, -100, int.MinValue };

        foreach (var count in negativeCounts)
        {
            // Act
            var result = _zeroIsTrueConverter.Convert(count, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for count: {count}");
                Assert.That(result, Is.TypeOf<bool>(), $"Failed for count: {count}");
                Assert.That((bool)result!, Is.False, $"Failed for count: {count}");
            });
        }
    }

    [Test]
    public void ZeroIsFalse_Convert_WithZero_Should_Return_False()
    {
        // Act
        var result = _zeroIsFalseConverter.Convert(0, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<bool>());
            Assert.That((bool)result!, Is.False);
        });
    }

    [Test]
    public void ZeroIsFalse_Convert_WithPositiveCount_Should_Return_True()
    {
        // Arrange
        var positiveCounts = new[] { 1, 2, 5, 10, 100, 999, int.MaxValue };

        foreach (var count in positiveCounts)
        {
            // Act
            var result = _zeroIsFalseConverter.Convert(count, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for count: {count}");
                Assert.That(result, Is.TypeOf<bool>(), $"Failed for count: {count}");
                Assert.That((bool)result!, Is.True, $"Failed for count: {count}");
            });
        }
    }

    [Test]
    public void ZeroIsFalse_Convert_WithNegativeCount_Should_Return_False()
    {
        // Arrange
        var negativeCounts = new[] { -1, -2, -5, -10, -100, int.MinValue };

        foreach (var count in negativeCounts)
        {
            // Act
            var result = _zeroIsFalseConverter.Convert(count, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for count: {count}");
                Assert.That(result, Is.TypeOf<bool>(), $"Failed for count: {count}");
                Assert.That((bool)result!, Is.False, $"Failed for count: {count}");
            });
        }
    }

    [Test]
    public void ZeroIsTrue_Convert_WithNonIntValue_Should_Return_True()
    {
        // Arrange
        var nonIntValues = new object[] { "0", "5", true, false, 3.14, null!, new() };

        foreach (var value in nonIntValues)
        {
            // Act
            var result = _zeroIsTrueConverter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for value: {value}");
                Assert.That(result, Is.TypeOf<bool>(), $"Failed for value: {value}");
                Assert.That((bool)result!, Is.True, $"Failed for value: {value}");
            });
        }
    }

    [Test]
    public void ZeroIsFalse_Convert_WithNonIntValue_Should_Return_False()
    {
        // Arrange
        var nonIntValues = new object[] { "0", "5", true, false, 3.14, null!, new() };

        foreach (var value in nonIntValues)
        {
            // Act
            var result = _zeroIsFalseConverter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Failed for value: {value}");
                Assert.That(result, Is.TypeOf<bool>(), $"Failed for value: {value}");
                Assert.That((bool)result!, Is.False, $"Failed for value: {value}");
            });
        }
    }

    [Test]
    public void Convert_WithDifferentTargetTypes_Should_Still_Work()
    {
        // Act & Assert
        var result1 = _zeroIsTrueConverter.Convert(0, typeof(object), null, CultureInfo.InvariantCulture);
        var result2 = _zeroIsTrueConverter.Convert(0, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.TypeOf<bool>());
            Assert.That(result2, Is.TypeOf<bool>());
            Assert.That((bool)result1!, Is.EqualTo((bool)result2!));
        });
    }

    [Test]
    public void Convert_WithDifferentParameters_Should_IgnoreParameter()
    {
        // Act
        var result1 = _zeroIsTrueConverter.Convert(5, typeof(bool), null, CultureInfo.InvariantCulture);
        var result2 = _zeroIsTrueConverter.Convert(5, typeof(bool), "some parameter", CultureInfo.InvariantCulture);

        // Assert
        Assert.That((bool)result1!, Is.EqualTo((bool)result2!));
    }

    [Test]
    public void Convert_WithDifferentCultures_Should_IgnoreCulture()
    {
        // Act
        var result1 = _zeroIsTrueConverter.Convert(3, typeof(bool), null, CultureInfo.InvariantCulture);
        var result2 = _zeroIsTrueConverter.Convert(3, typeof(bool), null, new CultureInfo("en-US"));
        var result3 = _zeroIsTrueConverter.Convert(3, typeof(bool), null, new CultureInfo("fi-FI"));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((bool)result1!, Is.EqualTo((bool)result2!));
            Assert.That((bool)result1!, Is.EqualTo((bool)result3!));
        });
    }

    [Test]
    public void ConvertBack_Should_Throw_NotImplementedException()
    {
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.Throws<System.NotImplementedException>(() =>
                _zeroIsTrueConverter.ConvertBack(true, typeof(int), null, CultureInfo.InvariantCulture));

            Assert.Throws<System.NotImplementedException>(() =>
                _zeroIsFalseConverter.ConvertBack(false, typeof(int), null, CultureInfo.InvariantCulture));
        });
    }

    [Test]
    public void Static_Instances_Convert_Should_Work_Correctly()
    {
        // Act & Assert for ZeroIsTrue
        var zeroIsTrueWith0 = CountToBooleanConverter.ZeroIsTrue.Convert(0, typeof(bool), null, CultureInfo.InvariantCulture);
        var zeroIsTrueWith5 = CountToBooleanConverter.ZeroIsTrue.Convert(5, typeof(bool), null, CultureInfo.InvariantCulture);

        // Act & Assert for ZeroIsFalse
        var zeroIsFalseWith0 = CountToBooleanConverter.ZeroIsFalse.Convert(0, typeof(bool), null, CultureInfo.InvariantCulture);
        var zeroIsFalseWith5 = CountToBooleanConverter.ZeroIsFalse.Convert(5, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That((bool)zeroIsTrueWith0!, Is.True);
            Assert.That((bool)zeroIsTrueWith5!, Is.False);
            Assert.That((bool)zeroIsFalseWith0!, Is.False);
            Assert.That((bool)zeroIsFalseWith5!, Is.True);
        });
    }

    [TestCase(0, true, true)]
    [TestCase(1, true, false)]
    [TestCase(5, true, false)]
    [TestCase(-1, true, false)]
    [TestCase(0, false, false)]
    [TestCase(1, false, true)]
    [TestCase(5, false, true)]
    [TestCase(-1, false, false)]
    public void Convert_WithTestCases_Should_Return_Expected_Values(int count, bool trueIfZero, bool expectedResult)
    {
        // Arrange
        var converter = new CountToBooleanConverter { TrueIfZero = trueIfZero };

        // Act
        var result = converter.Convert(count, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<bool>());
            Assert.That((bool)result!, Is.EqualTo(expectedResult));
        });
    }

    [Test]
    public void TrueIfZero_Property_Should_Be_Settable_Via_Init()
    {
        // Act
        var converter1 = new CountToBooleanConverter { TrueIfZero = true };
        var converter2 = new CountToBooleanConverter { TrueIfZero = false };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(converter1.TrueIfZero, Is.True);
            Assert.That(converter2.TrueIfZero, Is.False);
        });
    }

    [Test]
    public void Boundary_Values_Should_Be_Handled_Correctly()
    {
        // Test boundary integer values
        var testCases = new[]
        {
            (int.MaxValue, false, true),  // ZeroIsFalse with max positive
            (int.MinValue, false, false), // ZeroIsFalse with max negative
            (int.MaxValue, true, false),  // ZeroIsTrue with max positive
            (int.MinValue, true, false)   // ZeroIsTrue with max negative
        };

        foreach (var (value, trueIfZero, expected) in testCases)
        {
            var converter = new CountToBooleanConverter { TrueIfZero = trueIfZero };
            var result = converter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.That((bool)result!, Is.EqualTo(expected),
                $"Failed for value: {value}, TrueIfZero: {trueIfZero}");
        }
    }
}