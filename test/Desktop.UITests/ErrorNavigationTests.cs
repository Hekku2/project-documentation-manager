using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.NUnit;
using Desktop.Models;
using Desktop.ViewModels;
using Business.Models;
using Business.Services;
using Desktop.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Desktop.Configuration;

namespace Desktop.UITests;

[TestFixture]
public class ErrorNavigationTests
{
    [AvaloniaTest]
    public async Task UpdateErrorPanelWithValidationResults_CreatesErrorEntriesWithNavigation()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainWindowViewModel>>();
        var options = Substitute.For<IOptions<ApplicationOptions>>();
        var fileService = Substitute.For<IFileService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        var markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();

        options.Value.Returns(new ApplicationOptions 
        { 
            DefaultProjectFolder = "/test",
            DefaultOutputFolder = "/output"
        });

        var stateLogger = Substitute.For<ILogger<EditorStateService>>();
        var tabBarLogger = Substitute.For<ILogger<EditorTabBarViewModel>>();
        var contentLogger = Substitute.For<ILogger<EditorContentViewModel>>();
        
        var editorStateService = new EditorStateService(stateLogger);
        var editorTabBarViewModel = new EditorTabBarViewModel(tabBarLogger, fileService, editorStateService);
        var editorContentViewModel = new EditorContentViewModel(contentLogger, editorStateService, options, serviceProvider, markdownCombinationService, markdownFileCollectorService);
        
        var logTransitionService = Substitute.For<Desktop.Logging.ILogTransitionService>();
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var viewModel = new MainWindowViewModel(
            logger, 
            options, 
            fileService, 
            serviceProvider,
            editorStateService,
            editorTabBarViewModel,
            editorContentViewModel,
            logTransitionService,
            hotkeyService);

        var validationResult = new ValidationResult
        {
            Errors = 
            [
                new ValidationIssue
                {
                    Message = "Test error message",
                    DirectivePath = "/path/to/referenced.mdsrc",
                    SourceFile = "/path/to/template.mdext",
                    LineNumber = 5,
                    SourceContext = "Test context"
                }
            ],
            Warnings = 
            [
                new ValidationIssue
                {
                    Message = "Test warning message",
                    DirectivePath = "/path/to/warning.mdext",
                    SourceFile = "/path/to/template2.mdext",
                    LineNumber = 10
                }
            ]
        };

        // Act
        viewModel.UpdateErrorPanelWithValidationResults(validationResult);

        // Assert
        var errorTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "errors");
        Assert.That(errorTab, Is.Not.Null, "Error tab should be created");
        Assert.That(errorTab.ErrorEntries.Count, Is.EqualTo(2), "Should have 2 error entries");

        var errorEntry = errorTab.ErrorEntries[0];
        Assert.Multiple(() =>
        {
            Assert.That(errorEntry.Type, Is.EqualTo("Error"));
            Assert.That(errorEntry.Message, Is.EqualTo("Test error message"));
            // Should navigate to SourceFile (template.mdext) not DirectivePath (referenced.mdsrc)
            Assert.That(errorEntry.FilePath, Is.EqualTo("/path/to/template.mdext"));
            Assert.That(errorEntry.FileName, Is.EqualTo("template.mdext"));
            Assert.That(errorEntry.LineNumber, Is.EqualTo(5));
            Assert.That(errorEntry.SourceContext, Is.EqualTo("Test context"));
            Assert.That(errorEntry.HasFileNavigation, Is.True);
            Assert.That(errorEntry.NavigateCommand, Is.Not.Null);
        });

        var warningEntry = errorTab.ErrorEntries[1];
        Assert.Multiple(() =>
        {
            Assert.That(warningEntry.Type, Is.EqualTo("Warning"));
            Assert.That(warningEntry.Message, Is.EqualTo("Test warning message"));
            // Should navigate to SourceFile (template2.mdext) not DirectivePath (warning.mdext)
            Assert.That(warningEntry.FilePath, Is.EqualTo("/path/to/template2.mdext"));
            Assert.That(warningEntry.FileName, Is.EqualTo("template2.mdext"));
            Assert.That(warningEntry.LineNumber, Is.EqualTo(10));
            Assert.That(warningEntry.HasFileNavigation, Is.True);
            Assert.That(warningEntry.NavigateCommand, Is.Not.Null);
        });
    }

    [AvaloniaTest]
    public async Task UpdateErrorPanelWithValidationResults_FallsBackToDirectivePath_WhenSourceFileIsNull()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainWindowViewModel>>();
        var options = Substitute.For<IOptions<ApplicationOptions>>();
        var fileService = Substitute.For<IFileService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        var markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();

        options.Value.Returns(new ApplicationOptions 
        { 
            DefaultProjectFolder = "/test",
            DefaultOutputFolder = "/output"
        });

        var stateLogger = Substitute.For<ILogger<EditorStateService>>();
        var tabBarLogger = Substitute.For<ILogger<EditorTabBarViewModel>>();
        var contentLogger = Substitute.For<ILogger<EditorContentViewModel>>();
        
        var editorStateService = new EditorStateService(stateLogger);
        var editorTabBarViewModel = new EditorTabBarViewModel(tabBarLogger, fileService, editorStateService);
        var editorContentViewModel = new EditorContentViewModel(contentLogger, editorStateService, options, serviceProvider, markdownCombinationService, markdownFileCollectorService);
        
        var logTransitionService = Substitute.For<Desktop.Logging.ILogTransitionService>();
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var viewModel = new MainWindowViewModel(
            logger, 
            options, 
            fileService, 
            serviceProvider,
            editorStateService,
            editorTabBarViewModel,
            editorContentViewModel,
            logTransitionService,
            hotkeyService);

        var validationResult = new ValidationResult
        {
            Errors = 
            [
                new ValidationIssue
                {
                    Message = "Error with only DirectivePath",
                    DirectivePath = "/path/to/fallback.mdext",
                    SourceFile = null, // No SourceFile, should fallback to DirectivePath
                    LineNumber = 3
                }
            ]
        };

        // Act
        viewModel.UpdateErrorPanelWithValidationResults(validationResult);

        // Assert
        var errorTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "errors");
        Assert.That(errorTab, Is.Not.Null);
        
        var errorEntry = errorTab.ErrorEntries[0];
        Assert.Multiple(() =>
        {
            Assert.That(errorEntry.FilePath, Is.EqualTo("/path/to/fallback.mdext"));
            Assert.That(errorEntry.FileName, Is.EqualTo("fallback.mdext"));
            Assert.That(errorEntry.HasFileNavigation, Is.True);
        });
    }

    [AvaloniaTest]
    public void ErrorEntry_WithoutFilePath_DoesNotHaveNavigation()
    {
        // Arrange & Act
        var errorEntry = new ErrorEntry
        {
            Type = "Error",
            Message = "Error without file path",
            FilePath = null,
            FileName = null,
            LineNumber = null,
            SourceContext = null,
            NavigateCommand = null
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(errorEntry.HasFileNavigation, Is.False);
            Assert.That(errorEntry.FileDisplayText, Is.Null);
            Assert.That(errorEntry.DisplayText, Is.EqualTo("Error: Error without file path"));
        });
    }
}