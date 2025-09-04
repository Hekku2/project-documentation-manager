using Desktop.Controls;
using Desktop.Logging;
using Microsoft.Extensions.Logging;

namespace Desktop.UITests.Controls;

[TestFixture]
public class LogDisplayEntryTests
{
    private DateTime _testTimestamp;

    [SetUp]
    public void Setup()
    {
        _testTimestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123, DateTimeKind.Utc);
    }

    [Test]
    public void Constructor_WithValidLogEntry_Should_SetAllProperties()
    {
        // Arrange
        var logEntry = new LogEntry
        {
            Timestamp = _testTimestamp,
            LogLevel = LogLevel.Information,
            CategoryName = "TestCategory",
            Message = "Test message",
            Exception = null
        };

        // Act
        var displayEntry = new LogDisplayEntry(logEntry);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(displayEntry.LogEntry, Is.SameAs(logEntry));
            Assert.That(displayEntry.TimestampText, Is.EqualTo("[14:30:45.123] "));
            Assert.That(displayEntry.LevelText, Is.EqualTo("[INFO] "));
            Assert.That(displayEntry.LevelColor, Is.EqualTo("#4A9EFF"));
            Assert.That(displayEntry.CategoryText, Is.EqualTo("TestCategory: "));
            Assert.That(displayEntry.MessageText, Is.EqualTo("Test message"));
            Assert.That(displayEntry.ExceptionText, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void Constructor_WithException_Should_IncludeExceptionText()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception message");
        var logEntry = new LogEntry
        {
            Timestamp = _testTimestamp,
            LogLevel = LogLevel.Error,
            CategoryName = "TestCategory",
            Message = "Error occurred",
            Exception = exception
        };

        // Act
        var displayEntry = new LogDisplayEntry(logEntry);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(displayEntry.ExceptionText, Is.EqualTo("\n    Exception: Test exception message"));
            Assert.That(displayEntry.MessageText, Is.EqualTo("Error occurred"));
            Assert.That(displayEntry.LevelText, Is.EqualTo("[ERR ] "));
            Assert.That(displayEntry.LevelColor, Is.EqualTo("#FF6B6B"));
        });
    }

    [TestCase(LogLevel.Critical, "[CRIT] ", "#FF0000")]
    [TestCase(LogLevel.Error, "[ERR ] ", "#FF6B6B")]
    [TestCase(LogLevel.Warning, "[WARN] ", "#FFB347")]
    [TestCase(LogLevel.Information, "[INFO] ", "#4A9EFF")]
    [TestCase(LogLevel.Debug, "[DBG ] ", "#98FB98")]
    [TestCase(LogLevel.Trace, "[TRC ] ", "#DDA0DD")]
    public void Constructor_WithDifferentLogLevels_Should_SetCorrectTextAndColor(LogLevel logLevel, string expectedText, string expectedColor)
    {
        // Arrange
        var logEntry = new LogEntry
        {
            Timestamp = _testTimestamp,
            LogLevel = logLevel,
            CategoryName = "TestCategory",
            Message = "Test message"
        };

        // Act
        var displayEntry = new LogDisplayEntry(logEntry);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(displayEntry.LevelText, Is.EqualTo(expectedText));
            Assert.That(displayEntry.LevelColor, Is.EqualTo(expectedColor));
        });
    }

    [Test]
    public void Constructor_WithUnknownLogLevel_Should_UseDefaultValues()
    {
        // Arrange
        var logEntry = new LogEntry
        {
            Timestamp = _testTimestamp,
            LogLevel = (LogLevel)999, // Unknown log level
            CategoryName = "TestCategory",
            Message = "Test message"
        };

        // Act
        var displayEntry = new LogDisplayEntry(logEntry);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(displayEntry.LevelText, Is.EqualTo("[UNKN] "));
            Assert.That(displayEntry.LevelColor, Is.EqualTo("#CCCCCC"));
        });
    }

    [Test]
    public void TimestampText_Should_FormatCorrectly()
    {
        // Arrange
        var testCases = new[]
        {
            (new DateTime(2024, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc), "[23:59:59.999] "),
            (new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "[00:00:00.000] "),
            (new DateTime(2024, 6, 15, 12, 30, 45, 500, DateTimeKind.Utc), "[12:30:45.500] "),
            (new DateTime(2024, 3, 8, 9, 5, 3, 50, DateTimeKind.Utc), "[09:05:03.050] ")
        };

        foreach (var (timestamp, expectedFormat) in testCases)
        {
            var logEntry = new LogEntry
            {
                Timestamp = timestamp,
                LogLevel = LogLevel.Information,
                CategoryName = "Test",
                Message = "Test"
            };

            // Act
            var displayEntry = new LogDisplayEntry(logEntry);

            // Assert
            Assert.That(displayEntry.TimestampText, Is.EqualTo(expectedFormat),
                $"Failed for timestamp: {timestamp}");
        }
    }

    [Test]
    public void CategoryText_Should_IncludeColonAndSpace()
    {
        // Arrange
        var testCategories = new[] { "MyApp.Service", "System.Net.Http", "Microsoft.Hosting", "", "A" };

        foreach (var category in testCategories)
        {
            var logEntry = new LogEntry
            {
                Timestamp = _testTimestamp,
                LogLevel = LogLevel.Information,
                CategoryName = category,
                Message = "Test message"
            };

            // Act
            var displayEntry = new LogDisplayEntry(logEntry);

            // Assert
            Assert.That(displayEntry.CategoryText, Is.EqualTo($"{category}: "),
                $"Failed for category: '{category}'");
        }
    }

    [Test]
    public void MessageText_Should_PreserveOriginalMessage()
    {
        // Arrange
        var testMessages = new[]
        {
            "Simple message",
            "Message with\nnewlines\nand\ttabs",
            "Message with special chars: !@#$%^&*()",
            "",
            "   Leading and trailing spaces   ",
            "Unicode message: 🚀 ñáéíóú"
        };

        foreach (var message in testMessages)
        {
            var logEntry = new LogEntry
            {
                Timestamp = _testTimestamp,
                LogLevel = LogLevel.Information,
                CategoryName = "Test",
                Message = message
            };

            // Act
            var displayEntry = new LogDisplayEntry(logEntry);

            // Assert
            Assert.That(displayEntry.MessageText, Is.EqualTo(message),
                $"Failed for message: '{message}'");
        }
    }

    [Test]
    public void ExceptionText_WithNestedExceptions_Should_ShowOuterExceptionMessage()
    {
        // Arrange
        var innerException = new ArgumentException("Inner exception message");
        var outerException = new InvalidOperationException("Outer exception message", innerException);

        var logEntry = new LogEntry
        {
            Timestamp = _testTimestamp,
            LogLevel = LogLevel.Error,
            CategoryName = "Test",
            Message = "Error with nested exception",
            Exception = outerException
        };

        // Act
        var displayEntry = new LogDisplayEntry(logEntry);

        // Assert
        Assert.That(displayEntry.ExceptionText, Is.EqualTo("\n    Exception: Outer exception message"));
    }

    [Test]
    public void Properties_Should_BeReadOnly()
    {
        // Assert - Properties should not have setters (compile-time check)
        var type = typeof(LogDisplayEntry);

        Assert.Multiple(() =>
        {
            Assert.That(type.GetProperty(nameof(LogDisplayEntry.LogEntry))?.SetMethod, Is.Null);
            Assert.That(type.GetProperty(nameof(LogDisplayEntry.TimestampText))?.SetMethod, Is.Null);
            Assert.That(type.GetProperty(nameof(LogDisplayEntry.LevelText))?.SetMethod, Is.Null);
            Assert.That(type.GetProperty(nameof(LogDisplayEntry.LevelColor))?.SetMethod, Is.Null);
            Assert.That(type.GetProperty(nameof(LogDisplayEntry.CategoryText))?.SetMethod, Is.Null);
            Assert.That(type.GetProperty(nameof(LogDisplayEntry.MessageText))?.SetMethod, Is.Null);
            Assert.That(type.GetProperty(nameof(LogDisplayEntry.ExceptionText))?.SetMethod, Is.Null);
        });
    }

    [Test]
    public void Constructor_WithComplexException_Should_HandleExceptionMessage()
    {
        // Arrange
        Exception exception;
        try
        {
            throw new DivideByZeroException("Cannot divide by zero");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var logEntry = new LogEntry
        {
            Timestamp = _testTimestamp,
            LogLevel = LogLevel.Critical,
            CategoryName = "MathService",
            Message = "Critical calculation error",
            Exception = exception
        };

        // Act
        var displayEntry = new LogDisplayEntry(logEntry);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(displayEntry.ExceptionText, Contains.Substring("Cannot divide by zero"));
            Assert.That(displayEntry.ExceptionText, Does.StartWith("\n    Exception: "));
            Assert.That(displayEntry.LevelText, Is.EqualTo("[CRIT] "));
            Assert.That(displayEntry.LevelColor, Is.EqualTo("#FF0000"));
        });
    }

    [Test]
    public void LogEntry_Property_Should_ReturnSameInstance()
    {
        // Arrange
        var logEntry = new LogEntry
        {
            Timestamp = _testTimestamp,
            LogLevel = LogLevel.Debug,
            CategoryName = "Test",
            Message = "Debug message"
        };

        // Act
        var displayEntry = new LogDisplayEntry(logEntry);

        // Assert
        Assert.That(displayEntry.LogEntry, Is.SameAs(logEntry));
    }

    [Test]
    public void Constructor_WithAllLogLevels_Should_ProduceUniqueColors()
    {
        // Arrange
        var logLevels = new[]
        {
            LogLevel.Critical,
            LogLevel.Error,
            LogLevel.Warning,
            LogLevel.Information,
            LogLevel.Debug,
            LogLevel.Trace
        };

        var colors = new List<string>();

        // Act
        foreach (var level in logLevels)
        {
            var logEntry = new LogEntry
            {
                Timestamp = _testTimestamp,
                LogLevel = level,
                CategoryName = "Test",
                Message = "Test"
            };
            var displayEntry = new LogDisplayEntry(logEntry);
            colors.Add(displayEntry.LevelColor);
        }

        // Assert
        Assert.That(colors.Distinct().Count(), Is.EqualTo(logLevels.Length),
            "All log levels should have unique colors");
    }

    [Test]
    public void Constructor_WithAllLogLevels_Should_ProduceUniqueTexts()
    {
        // Arrange
        var logLevels = new[]
        {
            LogLevel.Critical,
            LogLevel.Error,
            LogLevel.Warning,
            LogLevel.Information,
            LogLevel.Debug,
            LogLevel.Trace
        };

        var texts = new List<string>();

        // Act
        foreach (var level in logLevels)
        {
            var logEntry = new LogEntry
            {
                Timestamp = _testTimestamp,
                LogLevel = level,
                CategoryName = "Test",
                Message = "Test"
            };
            var displayEntry = new LogDisplayEntry(logEntry);
            texts.Add(displayEntry.LevelText);
        }

        // Assert
        Assert.That(texts.Distinct().Count(), Is.EqualTo(logLevels.Length),
            "All log levels should have unique text representations");
    }

    [Test]
    public void LevelColors_Should_BeValidHexColors()
    {
        // Arrange
        var logLevels = Enum.GetValues<LogLevel>().Concat(new[] { (LogLevel)999 }); // Include unknown level

        foreach (var level in logLevels)
        {
            var logEntry = new LogEntry
            {
                Timestamp = _testTimestamp,
                LogLevel = level,
                CategoryName = "Test",
                Message = "Test"
            };

            // Act
            var displayEntry = new LogDisplayEntry(logEntry);

            // Assert
            Assert.That(displayEntry.LevelColor, Does.Match(@"^#[0-9A-Fa-f]{6}$"),
                $"Color for {level} should be a valid hex color");
        }
    }

    [Test]
    public void LevelTexts_Should_HaveConsistentFormat()
    {
        // Arrange
        var logLevels = Enum.GetValues<LogLevel>().Concat(new[] { (LogLevel)999 }); // Include unknown level

        foreach (var level in logLevels)
        {
            var logEntry = new LogEntry
            {
                Timestamp = _testTimestamp,
                LogLevel = level,
                CategoryName = "Test",
                Message = "Test"
            };

            // Act
            var displayEntry = new LogDisplayEntry(logEntry);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(displayEntry.LevelText, Does.StartWith("["),
                    $"Level text for {level} should start with '['");
                Assert.That(displayEntry.LevelText, Does.EndWith("] "),
                    $"Level text for {level} should end with '] '");
                Assert.That(displayEntry.LevelText, Has.Length.EqualTo(7),
                    $"Level text for {level} should be exactly 7 characters");
            });
        }
    }
}