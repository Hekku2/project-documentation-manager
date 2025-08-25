using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NSubstitute;
using Desktop.Logging;
using Desktop.ViewModels;

namespace Desktop.UITests;

[TestFixture]
public class LogTransitionServiceIntegrationTests
{
    [Test]
    public void MainWindowViewModel_Should_Use_Historical_Logs_For_Log_Tab()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainWindowViewModel>>();
        var options = Microsoft.Extensions.Options.Options.Create(new Desktop.Configuration.ApplicationOptions());
        var fileService = Substitute.For<Desktop.Services.IFileService>();
        var serviceProvider = Substitute.For<System.IServiceProvider>();
        var editorStateService = new Desktop.Services.EditorStateService(Substitute.For<ILogger<Desktop.Services.EditorStateService>>());
        var editorTabBarViewModel = new EditorTabBarViewModel(
            Substitute.For<ILogger<EditorTabBarViewModel>>(), 
            fileService, 
            editorStateService);
        var editorContentViewModel = new EditorContentViewModel(
            Substitute.For<ILogger<EditorContentViewModel>>(), 
            editorStateService, 
            options, 
            serviceProvider, 
            Substitute.For<Business.Services.IMarkdownCombinationService>(), 
            Substitute.For<Business.Services.IMarkdownFileCollectorService>());
        
        var mockLogTransitionService = Substitute.For<ILogTransitionService>();
        var testLogs = "Test historical logs from startup";
        mockLogTransitionService.GetFormattedHistoricalLogs().Returns(testLogs);
        
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var viewModel = new MainWindowViewModel(
            logger, 
            options, 
            editorStateService,
            editorTabBarViewModel,
            editorContentViewModel,
            mockLogTransitionService,
            hotkeyService);
        
        // Act
        viewModel.ShowLogsCommand.Execute(null);
        
        // Assert
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        Assert.That(logTab, Is.Not.Null, "Log tab should be created");
        Assert.That(logTab.Content, Is.EqualTo(testLogs + Environment.NewLine), "Log tab should contain historical logs with trailing newline");
        mockLogTransitionService.Received(1).GetFormattedHistoricalLogs();
    }
    
    [Test]
    public void LogTransitionService_Should_Store_Historical_Logs()
    {
        // Arrange
        var dynamicProvider = new DynamicLoggerProvider();
        var inMemoryProvider = new InMemoryLoggerProvider();
        var service = new LogTransitionService(dynamicProvider, inMemoryProvider);
        
        // Add some logs to the in-memory provider first
        var logger = inMemoryProvider.CreateLogger("TestCategory");
        logger.LogInformation("Startup log 1");
        logger.LogWarning("Startup warning");
        
        // Act - get historical logs before transition
        var historicalLogsFormatted = service.GetFormattedHistoricalLogs();
        
        // Assert
        Assert.That(historicalLogsFormatted, Does.Contain("Startup log 1"));
        Assert.That(historicalLogsFormatted, Does.Contain("Startup warning"));
        Assert.That(historicalLogsFormatted, Does.Contain("[INFO] TestCategory: Startup log 1"));
        Assert.That(historicalLogsFormatted, Does.Contain("[WARN] TestCategory: Startup warning"));
    }
}