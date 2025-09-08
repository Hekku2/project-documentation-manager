using NSubstitute;
using ProjectDocumentationManager.Business.Models;
using ProjectDocumentationManager.Business.Services;
using ProjectDocumentationManager.Console.Commands;
using Spectre.Console.Cli;

namespace ProjectDocumentationManager.Console.Tests.Commands;

[TestFixture]
public class ValidateCommandTests
{
    private ValidateCommand _command = null!;
    private IMarkdownFileCollectorService _collector = null!;
    private IMarkdownCombinationService _combiner = null!;
    private string _testInputFolder = null!;

    [SetUp]
    public void Setup()
    {
        _collector = Substitute.For<IMarkdownFileCollectorService>();
        _combiner = Substitute.For<IMarkdownCombinationService>();
        _command = new ValidateCommand(_collector, _combiner);

        _testInputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testInputFolder);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testInputFolder))
            Directory.Delete(_testInputFolder, true);
    }

    [Test]
    public void Settings_Should_Require_InputFolder()
    {
        var settings = new ValidateCommand.Settings
        {
            InputFolder = "/test/input"
        };

        Assert.That(settings.InputFolder, Is.EqualTo("/test/input"));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Error_When_InputFolder_Does_Not_Exist()
    {
        var settings = new ValidateCommand.Settings
        {
            InputFolder = "/nonexistent/folder"
        };

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Success_When_No_Files_Found()
    {
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((Enumerable.Empty<MarkdownDocument>(), Enumerable.Empty<MarkdownDocument>())));

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Success_When_All_Files_Are_Valid()
    {
        var templateDoc1 = new MarkdownDocument { FileName = "file1.mdext", Content = "Content 1", FilePath = "file1.mdext" };
        var templateDoc2 = new MarkdownDocument { FileName = "file2.mdext", Content = "Content 2", FilePath = "file2.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var templateFiles = new[] { templateDoc1, templateDoc2 };
        var sourceFiles = new[] { sourceDoc };

        var validResult = new ValidationResult();

        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((templateFiles.AsEnumerable(), sourceFiles.AsEnumerable())));

        _combiner.Validate(templateFiles, sourceFiles)
            .Returns(validResult);

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Error_When_Some_Files_Are_Invalid()
    {
        var templateDoc1 = new MarkdownDocument { FileName = "valid.mdext", Content = "Valid content", FilePath = "valid.mdext" };
        var templateDoc2 = new MarkdownDocument { FileName = "invalid.mdext", Content = "Invalid content", FilePath = "invalid.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var templateFiles = new[] { templateDoc1, templateDoc2 };
        var sourceFiles = new[] { sourceDoc };

        var invalidResult = new ValidationResult
        {
            Errors = [new ValidationIssue { Message = "Validation error", SourceFile = "invalid.mdext", LineNumber = 1 }]
        };

        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((templateFiles.AsEnumerable(), sourceFiles.AsEnumerable())));

        _combiner.Validate(templateFiles, sourceFiles)
            .Returns(invalidResult);

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Error_When_All_Files_Are_Invalid()
    {
        var templateDoc1 = new MarkdownDocument { FileName = "invalid1.mdext", Content = "Invalid content 1", FilePath = "invalid1.mdext" };
        var templateDoc2 = new MarkdownDocument { FileName = "invalid2.mdext", Content = "Invalid content 2", FilePath = "invalid2.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var templateFiles = new[] { templateDoc1, templateDoc2 };
        var sourceFiles = new[] { sourceDoc };

        var invalidResult = new ValidationResult
        {
            Errors = [
                new ValidationIssue { Message = "Error 1", SourceFile = "invalid1.mdext", LineNumber = 1 },
                new ValidationIssue { Message = "Error 2", SourceFile = "invalid2.mdext", LineNumber = 1 }
            ]
        };

        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((templateFiles.AsEnumerable(), sourceFiles.AsEnumerable())));

        _combiner.Validate(templateFiles, sourceFiles)
            .Returns(invalidResult);

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Call_Services_With_Correct_Parameters()
    {
        var templateDoc = new MarkdownDocument { FileName = "test.mdext", Content = "Test content", FilePath = "test.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var templateFiles = new[] { templateDoc };
        var sourceFiles = new[] { sourceDoc };

        var validResult = new ValidationResult();

        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((templateFiles.AsEnumerable(), sourceFiles.AsEnumerable())));

        _combiner.Validate(templateFiles, sourceFiles)
            .Returns(validResult);

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext(Substitute.For<IRemainingArguments>(), "validate", null);
        await _command.ExecuteAsync(context, settings);

        await Assert.MultipleAsync(async () =>
        {
            await _collector.Received(1).CollectAllMarkdownFilesAsync(_testInputFolder);
            _combiner.Received(1).Validate(templateFiles, sourceFiles);
        });
    }
}