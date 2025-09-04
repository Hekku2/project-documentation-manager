using Microsoft.Extensions.Logging;
using Desktop.Logging;

namespace Desktop.UITests;

[TestFixture]
public class InMemoryLoggingTests
{
    [Test]
    public void InMemoryLogger_Should_Store_Log_Entries()
    {
        // Arrange
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");

        // Act
        logger.LogInformation("Test message 1");
        logger.LogWarning("Test warning");
        logger.LogError("Test error");

        // Assert
        var entries = provider.GetLogEntries().ToList();
        Assert.That(entries, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(entries[0].Message, Is.EqualTo("Test message 1"));
            Assert.That(entries[0].LogLevel, Is.EqualTo(LogLevel.Information));
            Assert.That(entries[1].Message, Is.EqualTo("Test warning"));
            Assert.That(entries[1].LogLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(entries[2].Message, Is.EqualTo("Test error"));
            Assert.That(entries[2].LogLevel, Is.EqualTo(LogLevel.Error));
        });
    }

    [Test]
    public void InMemoryLogger_Should_Format_Log_Entries_Correctly()
    {
        // Arrange
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");

        // Act
        logger.LogInformation("Test message");

        // Assert
        var formattedLogs = provider.GetFormattedLogs();
        Assert.That(formattedLogs, Does.Contain("[INFO] TestCategory: Test message"));
    }

    [Test]
    public void InMemoryLogger_Should_Clear_Logs()
    {
        // Arrange
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");
        logger.LogInformation("Test message");

        // Act
        provider.ClearLogs();

        // Assert
        var entries = provider.GetLogEntries();
        Assert.That(entries, Is.Empty);
    }

    [Test]
    public void LogEntry_Should_Format_With_Exception()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = LogLevel.Error,
            CategoryName = "TestCategory",
            Message = "Error occurred",
            Exception = exception
        };

        // Act
        var formatted = logEntry.FormatLogEntry();

        // Assert
        Assert.That(formatted, Does.Contain("[ERR ] TestCategory: Error occurred"));
        Assert.That(formatted, Does.Contain("Exception: Test exception"));
    }
}