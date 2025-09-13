using NSubstitute;
using ProjectDocumentationManager.Business.Models;
using ProjectDocumentationManager.Business.Services;
using ProjectDocumentationManager.Console.Commands;
using ProjectDocumentationManager.Console.Services;
using Spectre.Console.Cli;

namespace ProjectDocumentationManager.Console.Tests.Commands;

[TestFixture]
public class CombineCommandTests
{
    private CombineCommand _command = null!;
    private IMarkdownFileCollectorService _collector = null!;
    private IMarkdownCombinationService _combiner = null!;
    private IMarkdownDocumentFileWriterService _writer = null!;
    private Spectre.Console.IAnsiConsole _ansiConsole = null!;
    private IFileSystemService _fileSystemService = null!;
    private string _testInputFolder = null!;
    private string _testOutputFolder = null!;

    [SetUp]
    public void Setup()
    {
        _collector = Substitute.For<IMarkdownFileCollectorService>();
        _combiner = Substitute.For<IMarkdownCombinationService>();
        _writer = Substitute.For<IMarkdownDocumentFileWriterService>();
        _ansiConsole = Substitute.For<Spectre.Console.IAnsiConsole>();
        _fileSystemService = Substitute.For<IFileSystemService>();
        _command = new CombineCommand(_collector, _combiner, _writer, _ansiConsole, _fileSystemService);

        _testInputFolder = "/test/input/folder";
        _testOutputFolder = "/test/output/folder";
    }

    [Test]
    public void Settings_Should_Require_InputFolder()
    {
        var settings = new CombineCommand.Settings
        {
            InputFolder = "/test/input",
            OutputFolder = "/test/output"
        };

        Assert.Multiple(() =>
        {
            Assert.That(settings.InputFolder, Is.EqualTo("/test/input"));
            Assert.That(settings.OutputFolder, Is.EqualTo("/test/output"));
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Error_When_InputFolder_Does_Not_Exist()
    {
        var settings = new CombineCommand.Settings
        {
            InputFolder = "/nonexistent/folder",
            OutputFolder = _testOutputFolder
        };

        _fileSystemService.DirectoryExists("/nonexistent/folder").Returns(false);

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "combine", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Error_When_No_Files_Found()
    {
        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((Enumerable.Empty<MarkdownDocument>(), Enumerable.Empty<MarkdownDocument>())));

        var settings = new CombineCommand.Settings
        {
            InputFolder = _testInputFolder,
            OutputFolder = _testOutputFolder
        };

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "combine", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Process_Valid_Files_Successfully()
    {
        var templateDoc = new MarkdownDocument { FileName = "test.mdext", Content = "Test content", FilePath = "test.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };
        var processedDoc = new MarkdownDocument { FileName = "test.md", Content = "Processed content", FilePath = "test.md" };

        var templateFiles = new[] { templateDoc };
        var sourceFiles = new[] { sourceDoc };
        var processedFiles = new[] { processedDoc };

        var validResult = new ValidationResult();

        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _fileSystemService.GetFullPath(_testOutputFolder).Returns("/full/path/to/output");
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((templateFiles.AsEnumerable(), sourceFiles.AsEnumerable())));

        _combiner.BuildDocumentation(templateFiles, sourceFiles)
            .Returns(processedFiles);

        _combiner.Validate(templateFiles, sourceFiles)
            .Returns(validResult);

        var settings = new CombineCommand.Settings
        {
            InputFolder = _testInputFolder,
            OutputFolder = _testOutputFolder
        };

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "combine", null);
        var result = await _command.ExecuteAsync(context, settings);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(result, Is.EqualTo(0));
            _fileSystemService.Received(1).EnsureDirectoryExists(_testOutputFolder);
            await _writer.Received(1).WriteDocumentsToFolderAsync(
                Arg.Any<IEnumerable<MarkdownDocument>>(),
                _testOutputFolder);
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Handle_Invalid_Files()
    {
        var templateDoc = new MarkdownDocument { FileName = "invalid.mdext", Content = "Invalid content", FilePath = "invalid.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var templateFiles = new[] { templateDoc };
        var sourceFiles = new[] { sourceDoc };

        var invalidResult = new ValidationResult
        {
            Errors = [new ValidationIssue { Message = "Test error", SourceFile = "invalid.mdext", LineNumber = 1 }]
        };

        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((templateFiles.AsEnumerable(), sourceFiles.AsEnumerable())));

        _combiner.Validate(templateFiles, sourceFiles)
            .Returns(invalidResult);

        var settings = new CombineCommand.Settings
        {
            InputFolder = _testInputFolder,
            OutputFolder = _testOutputFolder
        };

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "combine", null);
        var result = await _command.ExecuteAsync(context, settings);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(result, Is.EqualTo(1)); // Command fails with validation errors
            await _writer.DidNotReceive().WriteDocumentsToFolderAsync(
                Arg.Any<IEnumerable<MarkdownDocument>>(),
                Arg.Any<string>());
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Create_Output_Directory()
    {
        var templateDoc = new MarkdownDocument { FileName = "test.mdext", Content = "Test content", FilePath = "test.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };
        var processedDoc = new MarkdownDocument { FileName = "test.md", Content = "Processed content", FilePath = "test.md" };

        var templateFiles = new[] { templateDoc };
        var sourceFiles = new[] { sourceDoc };
        var processedFiles = new[] { processedDoc };

        var validResult = new ValidationResult();

        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((templateFiles.AsEnumerable(), sourceFiles.AsEnumerable())));

        _combiner.BuildDocumentation(templateFiles, sourceFiles)
            .Returns(processedFiles);

        _combiner.Validate(templateFiles, sourceFiles)
            .Returns(validResult);

        var settings = new CombineCommand.Settings
        {
            InputFolder = _testInputFolder,
            OutputFolder = _testOutputFolder
        };

        var context = new CommandContext(["combine"], Substitute.For<IRemainingArguments>(), "combine", null);
        await _command.ExecuteAsync(context, settings);

        _fileSystemService.Received(1).EnsureDirectoryExists(_testOutputFolder);
    }
}