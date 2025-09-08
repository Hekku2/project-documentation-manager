using NSubstitute;
using ProjectDocumentationManager.Business.Models;
using ProjectDocumentationManager.Business.Services;
using ProjectDocumentationManager.Console.Commands;
using Spectre.Console.Cli;

namespace ProjectDocumentationManager.Console.Tests.Commands;

[TestFixture]
public class CombineCommandTests
{
    private CombineCommand _command = null!;
    private IMarkdownFileCollectorService _collector = null!;
    private IMarkdownCombinationService _combiner = null!;
    private IMarkdownDocumentFileWriterService _writer = null!;
    private string _testInputFolder = null!;
    private string _testOutputFolder = null!;

    [SetUp]
    public void Setup()
    {
        _collector = Substitute.For<IMarkdownFileCollectorService>();
        _combiner = Substitute.For<IMarkdownCombinationService>();
        _writer = Substitute.For<IMarkdownDocumentFileWriterService>();
        _command = new CombineCommand(_collector, _combiner, _writer);

        _testInputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _testOutputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(_testInputFolder);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testInputFolder))
            Directory.Delete(_testInputFolder, true);

        if (Directory.Exists(_testOutputFolder))
            Directory.Delete(_testOutputFolder, true);
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

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "combine", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Error_When_No_Files_Found()
    {
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((Enumerable.Empty<MarkdownDocument>(), Enumerable.Empty<MarkdownDocument>())));

        var settings = new CombineCommand.Settings
        {
            InputFolder = _testInputFolder,
            OutputFolder = _testOutputFolder
        };

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "combine", null);
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

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "combine", null);
        var result = await _command.ExecuteAsync(context, settings);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(result, Is.EqualTo(0));
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

        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((templateFiles.AsEnumerable(), sourceFiles.AsEnumerable())));

        _combiner.Validate(templateFiles, sourceFiles)
            .Returns(invalidResult);

        var settings = new CombineCommand.Settings
        {
            InputFolder = _testInputFolder,
            OutputFolder = _testOutputFolder
        };

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "combine", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.Multiple(async () =>
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

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "combine", null);
        await _command.ExecuteAsync(context, settings);

        Assert.That(Directory.Exists(_testOutputFolder), Is.True);
    }
}