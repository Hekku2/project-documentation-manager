using Avalonia.Headless.NUnit;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Business.Services;
using Business.Models;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace Desktop.UITests.ViewModels;

[NonParallelizable]
public class BuildConfirmationDialogViewModelTests
{
    private IOptions<ApplicationOptions> _options = null!;
    private IMarkdownFileCollectorService _mockFileCollector = null!;
    private IMarkdownCombinationService _mockCombination = null!;
    private IMarkdownDocumentFileWriterService _mockFileWriter = null!;
    private ILogger<BuildConfirmationDialogViewModel> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _options = Options.Create(new ApplicationOptions());
        _mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        _mockCombination = Substitute.For<IMarkdownCombinationService>();
        _mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        _mockLogger = NullLoggerFactory.Instance.CreateLogger<BuildConfirmationDialogViewModel>();
    }

    private BuildConfirmationDialogViewModel CreateDialogViewModel(ApplicationOptions? customOptions = null)
    {
        var optionsToUse = customOptions != null ? Options.Create(customOptions) : _options;
        return new BuildConfirmationDialogViewModel(optionsToUse, _mockFileCollector, _mockCombination, _mockFileWriter, _mockLogger);
    }

    private static async Task WaitForConditionAsync(System.Func<bool> condition, int timeoutMs = 2000, int intervalMs = 10)
    {
        var maxWait = timeoutMs / intervalMs;
        var waitCount = 0;
        while (!condition() && waitCount < maxWait)
        {
            await Task.Delay(intervalMs);
            waitCount++;
        }
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Show_Combined_Project_And_Output_Path()
    {
        var customOptions = new ApplicationOptions 
        { 
            DefaultProjectFolder = "/test/project",
            DefaultOutputFolder = "test-output" 
        };
        
        var dialogViewModel = CreateDialogViewModel(customOptions);
        
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.OutputLocation, Is.Not.Null.And.Not.Empty, "Output location should be set");
            Assert.That(dialogViewModel.OutputLocation, Is.EqualTo("/test/project/test-output"), "Should combine project folder and output folder");
            Assert.That(dialogViewModel.OutputLocation, Does.StartWith("/test/project"), "Should start with project folder");
            Assert.That(dialogViewModel.OutputLocation, Does.EndWith("test-output"), "Should end with output folder");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Have_Cancel_And_Compile_Commands()
    {
        var dialogViewModel = CreateDialogViewModel();
        
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.CancelCommand, Is.Not.Null, "Cancel command should exist");
            Assert.That(dialogViewModel.SaveCommand, Is.Not.Null, "Compile command should exist");
            Assert.That(dialogViewModel.CancelCommand.CanExecute(null), Is.True, "Cancel command should be executable");
            Assert.That(dialogViewModel.SaveCommand.CanExecute(null), Is.True, "Compile command should be enabled when not building");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Trigger_DialogClosed_Event_On_Cancel()
    {
        var dialogViewModel = CreateDialogViewModel();
        
        // Track if dialog closed event was triggered
        bool dialogClosed = false;
        dialogViewModel.DialogClosed += (sender, e) => dialogClosed = true;
        
        // Execute the cancel command
        dialogViewModel.CancelCommand.Execute(null);
        
        Assert.That(dialogClosed, Is.True, "DialogClosed event should be triggered when Cancel command is executed");
    }

    [AvaloniaTest]
    public async Task BuildConfirmationDialog_Should_Execute_Build_Process_When_Save_Is_Called()
    {
        // Arrange
        var customOptions = new ApplicationOptions
        {
            DefaultProjectFolder = "/test/project",
            DefaultOutputFolder = "output"
        };

        // Setup mock return values
        var templateFiles = new[]
        {
            new MarkdownDocument
            {
                FileName = "template1.mdext",
                FilePath = "/test/template1.mdext",
                Content = "# Template 1\n<insert source1.mdsrc>"
            },
            new MarkdownDocument
            {
                FileName = "template2.mdext",
                FilePath = "/test/template2.mdext",
                Content = "# Template 2"
            }
        };
        
        var sourceFiles = new[]
        {
            new MarkdownDocument
            {
                FileName = "source1.mdsrc",
                FilePath = "/test/source1.mdsrc",
                Content = "Source 1 content"
            }
        };

        var processedDocuments = new[]
        {
            new MarkdownDocument
            {
                FileName = "template1.md",
                FilePath = "/test/template1.md",
                Content = "# Template 1\nSource 1 content"
            },
            new MarkdownDocument
            {
                FileName = "template2.md",
                FilePath = "/test/template2.md",
                Content = "# Template 2"
            }
        };

        _mockFileCollector.CollectAllMarkdownFilesAsync("/test/project")
            .Returns((templateFiles, sourceFiles));
        _mockCombination.Validate(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(new ValidationResult());
        _mockCombination.BuildDocumentation(templateFiles, sourceFiles)
            .Returns(processedDocuments);
        _mockFileWriter.WriteDocumentsToFolderAsync(processedDocuments, "/test/project/output")
            .Returns(Task.CompletedTask);

        var dialogViewModel = CreateDialogViewModel(customOptions);

        // Track validation results event
        ValidationResult? receivedValidationResult = null;
        dialogViewModel.ValidationResultsAvailable += (sender, result) => receivedValidationResult = result;

        // Act
        dialogViewModel.SaveCommand.Execute(null);
        
        // Wait for async build to complete
        await WaitForConditionAsync(() => !dialogViewModel.IsBuildInProgress, 1000);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.CanBuild, Is.True, "Should be able to build again after completion");
            Assert.That(dialogViewModel.BuildStatus, Does.Contain("completed"), "Should show completion status");
            Assert.That(receivedValidationResult, Is.Not.Null, "ValidationResultsAvailable event should be triggered");
            Assert.That(receivedValidationResult!.IsValid, Is.True, "Validation should pass with no errors");

            // Verify services were called correctly
            _mockFileCollector.Received(1).CollectAllMarkdownFilesAsync("/test/project");
            _mockCombination.Received(1).Validate(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>());
            _mockCombination.Received(1).BuildDocumentation(templateFiles, sourceFiles);
            _mockFileWriter.Received(1).WriteDocumentsToFolderAsync(processedDocuments, "/test/project/output");
        });
    }

    [AvaloniaTest]
    public async Task BuildConfirmationDialog_Should_Handle_Build_Errors_Gracefully()
    {
        // Arrange
        var customOptions = new ApplicationOptions
        {
            DefaultProjectFolder = "/test/project",
            DefaultOutputFolder = "output"
        };

        // Setup mock to throw exception
        _mockFileCollector.CollectAllMarkdownFilesAsync("/test/project")
            .Returns(Task.FromException<(IEnumerable<MarkdownDocument>, IEnumerable<MarkdownDocument>)>(
                new DirectoryNotFoundException("Test directory not found")));

        var dialogViewModel = CreateDialogViewModel(customOptions);

        // Act
        dialogViewModel.SaveCommand.Execute(null);
        
        // Wait for async build to complete
        await WaitForConditionAsync(() => !dialogViewModel.IsBuildInProgress, 1000);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.CanBuild, Is.True, "Should be able to build again after error");
            Assert.That(dialogViewModel.BuildStatus, Does.Contain("failed"), "Should show failure status");
            Assert.That(dialogViewModel.BuildStatus, Does.Contain("Test directory not found"), "Should show error message");

            // Verify only file collector was called
            _mockFileCollector.Received(1).CollectAllMarkdownFilesAsync("/test/project");
            _mockCombination.DidNotReceive().BuildDocumentation(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>());
            _mockFileWriter.DidNotReceive().WriteDocumentsToFolderAsync(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<string>());
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Disable_Build_During_Build_Process()
    {
        // Arrange
        var dialogViewModel = CreateDialogViewModel();

        // Act
        dialogViewModel.IsBuildInProgress = true;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.CanBuild, Is.False, "Should not be able to build when build is in progress");
            Assert.That(dialogViewModel.SaveCommand.CanExecute(null), Is.False, "Compile command should be disabled during build");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Update_Build_Status_Property()
    {
        // Arrange
        var dialogViewModel = CreateDialogViewModel();

        // Track property changes
        string? capturedStatus = null;
        dialogViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(dialogViewModel.BuildStatus))
            {
                capturedStatus = dialogViewModel.BuildStatus;
            }
        };

        // Act
        dialogViewModel.BuildStatus = "Test status message";

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.BuildStatus, Is.EqualTo("Test status message"), "BuildStatus property should be updated");
            Assert.That(capturedStatus, Is.EqualTo("Test status message"), "PropertyChanged should be raised for BuildStatus");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Save_Button_Should_Be_Enabled_When_Build_Not_In_Progress()
    {
        // Arrange
        var dialogViewModel = CreateDialogViewModel();

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.IsBuildInProgress, Is.False, "Build should not be in progress initially");
            Assert.That(dialogViewModel.CanBuild, Is.True, "Should be able to build when not in progress");
            Assert.That(dialogViewModel.SaveCommand.CanExecute(null), Is.True, "Compile command should be enabled when build is not in progress");
        });
    }
}