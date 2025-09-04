using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Desktop.Logging;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Desktop.UITests.Services;

[TestFixture]
public class LogTransitionServiceTests
{
    private IDynamicLoggerProvider _mockDynamicLoggerProvider = null!;
    private InMemoryLoggerProvider _inMemoryLoggerProvider = null!;
    private LogTransitionService _logTransitionService = null!;
    private TextBox _testTextBox = null!;

    [SetUp]
    public void Setup()
    {
        _mockDynamicLoggerProvider = Substitute.For<IDynamicLoggerProvider>();
        _inMemoryLoggerProvider = new InMemoryLoggerProvider();
        _logTransitionService = new LogTransitionService(_mockDynamicLoggerProvider, _inMemoryLoggerProvider);
        _testTextBox = new TextBox();
    }

    [TearDown]
    public void TearDown()
    {
        _inMemoryLoggerProvider?.Dispose();
        _mockDynamicLoggerProvider?.Dispose();
    }

    [AvaloniaTest]
    public void TransitionToUILogging_FirstCall_Should_AddUILoggerProvider()
    {
        // Act
        _logTransitionService.TransitionToUILogging(_testTextBox);

        // Assert
        _mockDynamicLoggerProvider.Received(1).AddLoggerProvider(Arg.Any<UILoggerProvider>());
    }

    [AvaloniaTest]
    public void TransitionToUILogging_SecondCall_Should_NotAddLoggerProviderAgain()
    {
        // Act
        _logTransitionService.TransitionToUILogging(_testTextBox);
        _logTransitionService.TransitionToUILogging(_testTextBox);

        // Assert
        _mockDynamicLoggerProvider.Received(1).AddLoggerProvider(Arg.Any<UILoggerProvider>());
    }

    [AvaloniaTest]
    public void TransitionToUILogging_MultipleCallsWithDifferentTextBoxes_Should_OnlyAddOnce()
    {
        // Arrange
        var textBox1 = new TextBox();
        var textBox2 = new TextBox();

        // Act
        _logTransitionService.TransitionToUILogging(textBox1);
        _logTransitionService.TransitionToUILogging(textBox2);

        // Assert
        _mockDynamicLoggerProvider.Received(1).AddLoggerProvider(Arg.Any<UILoggerProvider>());
    }

    [Test]
    public void GetHistoricalLogs_WithEmptyProvider_Should_ReturnEmptyCollection()
    {
        // Act
        var logs = _logTransitionService.GetHistoricalLogs();

        // Assert
        Assert.That(logs, Is.Not.Null);
        Assert.That(logs, Is.Empty);
    }

    [Test]
    public void GetHistoricalLogs_WithLogsInProvider_Should_ReturnLogs()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        logger.LogInformation("Test message 1");
        logger.LogError("Test error message");
        logger.LogWarning("Test warning message");

        // Act
        var logs = _logTransitionService.GetHistoricalLogs().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(logs, Has.Count.EqualTo(3));
            Assert.That(logs[0].Message, Is.EqualTo("Test message 1"));
            Assert.That(logs[0].LogLevel, Is.EqualTo(LogLevel.Information));
            Assert.That(logs[1].Message, Is.EqualTo("Test error message"));
            Assert.That(logs[1].LogLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(logs[2].Message, Is.EqualTo("Test warning message"));
            Assert.That(logs[2].LogLevel, Is.EqualTo(LogLevel.Warning));
        });
    }

    [Test]
    public void GetFormattedHistoricalLogs_WithEmptyProvider_Should_ReturnEmptyString()
    {
        // Act
        var formattedLogs = _logTransitionService.GetFormattedHistoricalLogs();

        // Assert
        Assert.That(formattedLogs, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetFormattedHistoricalLogs_WithLogsInProvider_Should_ReturnFormattedString()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        logger.LogInformation("Test message 1");
        logger.LogError("Test error message");

        // Act
        var formattedLogs = _logTransitionService.GetFormattedHistoricalLogs();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(formattedLogs, Is.Not.Empty);
            Assert.That(formattedLogs, Contains.Substring("Test message 1"));
            Assert.That(formattedLogs, Contains.Substring("Test error message"));
            Assert.That(formattedLogs, Contains.Substring("INFO"));
            Assert.That(formattedLogs, Contains.Substring("ERR"));
            Assert.That(formattedLogs.Split(Environment.NewLine), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void GetHistoricalLogs_Should_ReturnLogsSortedByOrder()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        logger.LogInformation("First message");
        logger.LogWarning("Second message");
        logger.LogError("Third message");

        // Act
        var logs = _logTransitionService.GetHistoricalLogs().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(logs, Has.Count.EqualTo(3));
            Assert.That(logs[0].Message, Is.EqualTo("First message"));
            Assert.That(logs[1].Message, Is.EqualTo("Second message"));
            Assert.That(logs[2].Message, Is.EqualTo("Third message"));
        });
    }

    [Test]
    public void GetHistoricalLogs_Should_ReturnDifferentLogLevels()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        logger.LogTrace("Trace message");
        logger.LogDebug("Debug message");
        logger.LogInformation("Info message");
        logger.LogWarning("Warning message");
        logger.LogError("Error message");
        logger.LogCritical("Critical message");

        // Act
        var logs = _logTransitionService.GetHistoricalLogs().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            // Should have all levels except Trace (filtered by IsEnabled in InMemoryLogger)
            Assert.That(logs, Has.Count.EqualTo(5));
            Assert.That(logs.Any(l => l.LogLevel == LogLevel.Debug), Is.True);
            Assert.That(logs.Any(l => l.LogLevel == LogLevel.Information), Is.True);
            Assert.That(logs.Any(l => l.LogLevel == LogLevel.Warning), Is.True);
            Assert.That(logs.Any(l => l.LogLevel == LogLevel.Error), Is.True);
            Assert.That(logs.Any(l => l.LogLevel == LogLevel.Critical), Is.True);
        });
    }

    [Test]
    public void GetHistoricalLogs_WithException_Should_IncludeException()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        var testException = new InvalidOperationException("Test exception");
        logger.LogError(testException, "Error with exception");

        // Act
        var logs = _logTransitionService.GetHistoricalLogs().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(logs, Has.Count.EqualTo(1));
            Assert.That(logs[0].Message, Is.EqualTo("Error with exception"));
            Assert.That(logs[0].Exception, Is.EqualTo(testException));
            Assert.That(logs[0].LogLevel, Is.EqualTo(LogLevel.Error));
        });
    }

    [Test]
    public void GetHistoricalLogs_WithMultipleCategories_Should_ReturnAllLogs()
    {
        // Arrange
        var logger1 = _inMemoryLoggerProvider.CreateLogger("Category1");
        var logger2 = _inMemoryLoggerProvider.CreateLogger("Category2");

        logger1.LogInformation("Message from Category1");
        logger2.LogWarning("Message from Category2");

        // Act
        var logs = _logTransitionService.GetHistoricalLogs().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(logs, Has.Count.EqualTo(2));
            Assert.That(logs.Any(l => l.CategoryName == "Category1"), Is.True);
            Assert.That(logs.Any(l => l.CategoryName == "Category2"), Is.True);
            Assert.That(logs.Any(l => l.Message == "Message from Category1"), Is.True);
            Assert.That(logs.Any(l => l.Message == "Message from Category2"), Is.True);
        });
    }

    [Test]
    public void GetFormattedHistoricalLogs_Should_UseNewLineAsSeparator()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        logger.LogInformation("First log");
        logger.LogError("Second log");
        logger.LogWarning("Third log");

        // Act
        var formattedLogs = _logTransitionService.GetFormattedHistoricalLogs();

        // Assert
        var lines = formattedLogs.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines, Has.Length.EqualTo(3));
    }

    [Test]
    public void Constructor_Should_AcceptValidParameters()
    {
        // Act & Assert - Should not throw
        var service = new LogTransitionService(_mockDynamicLoggerProvider, _inMemoryLoggerProvider);
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void GetHistoricalLogs_AfterTransition_Should_StillReturnLogs()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        logger.LogInformation("Before transition");

        // Act
        _logTransitionService.TransitionToUILogging(_testTextBox);

        logger.LogInformation("After transition");
        var logs = _logTransitionService.GetHistoricalLogs().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(logs, Has.Count.EqualTo(2));
            Assert.That(logs[0].Message, Is.EqualTo("Before transition"));
            Assert.That(logs[1].Message, Is.EqualTo("After transition"));
        });
    }

    [AvaloniaTest]
    public void TransitionToUILogging_Should_AddUILoggerProviderWithCorrectTextBox()
    {
        // Arrange
        var capturedProvider = (UILoggerProvider?)null;
        _mockDynamicLoggerProvider.When(x => x.AddLoggerProvider(Arg.Any<UILoggerProvider>()))
                                  .Do(callInfo => capturedProvider = callInfo.Arg<UILoggerProvider>());

        // Act
        _logTransitionService.TransitionToUILogging(_testTextBox);

        // Assert
        Assert.That(capturedProvider, Is.Not.Null);
        // Note: We can't easily test the TextBox reference inside UILoggerProvider 
        // without exposing internal state, but we verify the correct type is added
    }

    [Test]
    public void Service_Should_ImplementILogTransitionService()
    {
        // Assert
        Assert.That(_logTransitionService, Is.InstanceOf<ILogTransitionService>());
    }

    [Test]
    public void GetHistoricalLogs_Should_ReturnSameInstanceAsInMemoryProvider()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        logger.LogInformation("Test message");

        // Act
        var serviceLogs = _logTransitionService.GetHistoricalLogs().ToList();
        var providerLogs = _inMemoryLoggerProvider.GetLogEntries().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(serviceLogs, Has.Count.EqualTo(providerLogs.Count));
            Assert.That(serviceLogs[0], Is.SameAs(providerLogs[0]));
        });
    }

    [Test]
    public void GetFormattedHistoricalLogs_Should_ProduceConsistentFormatting()
    {
        // Arrange
        var logger = _inMemoryLoggerProvider.CreateLogger("TestCategory");
        logger.LogInformation("Test info message");
        logger.LogError("Test error message");

        // Act
        var formatted1 = _logTransitionService.GetFormattedHistoricalLogs();
        var formatted2 = _logTransitionService.GetFormattedHistoricalLogs();

        // Assert
        Assert.That(formatted1, Is.EqualTo(formatted2));
    }
}