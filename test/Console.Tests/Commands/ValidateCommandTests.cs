using NSubstitute;
using MarkdownCompiler.Console.Models;
using MarkdownCompiler.Console.Commands;
using MarkdownCompiler.Console.Services;
using Spectre.Console.Cli;

namespace MarkdownCompiler.Console.Tests.Commands;

[TestFixture]
public class ValidateCommandTests
{
    private ValidateCommand _command = null!;
    private Spectre.Console.IAnsiConsole _ansiConsole = null!;
    private IMarkdownFileCollectorService _collector = null!;
    private IMarkdownCompilerService _compiler = null!;
    private IFileSystemService _fileSystemService = null!;
    private string _testInputFolder = null!;

    [SetUp]
    public void Setup()
    {
        _ansiConsole = Substitute.For<Spectre.Console.IAnsiConsole>();
        _collector = Substitute.For<IMarkdownFileCollectorService>();
        _compiler = Substitute.For<IMarkdownCompilerService>();
        _fileSystemService = Substitute.For<IFileSystemService>();
        _command = new ValidateCommand(_ansiConsole, _collector, _compiler, _fileSystemService);

        _testInputFolder = "/test/input/folder";
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

        _fileSystemService.DirectoryExists("/nonexistent/folder").Returns(false);

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Success_When_No_Files_Found()
    {
        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult(Enumerable.Empty<MarkdownDocument>()));

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Success_When_All_Files_Are_Valid()
    {
        var templateDoc1 = new MarkdownDocument { FileName = "file1.mdext", Content = "Content 1", FilePath = "file1.mdext" };
        var templateDoc2 = new MarkdownDocument { FileName = "file2.mdext", Content = "Content 2", FilePath = "file2.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var validResult = new ValidationResult();

        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((IEnumerable<MarkdownDocument>)[templateDoc1, templateDoc2, sourceDoc]));

        _compiler.Validate(Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(validResult);

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Error_When_Some_Files_Are_Invalid()
    {
        var templateDoc1 = new MarkdownDocument { FileName = "valid.mdext", Content = "Valid content", FilePath = "valid.mdext" };
        var templateDoc2 = new MarkdownDocument { FileName = "invalid.mdext", Content = "Invalid content", FilePath = "invalid.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var invalidResult = new ValidationResult
        {
            Errors = [new ValidationIssue { Message = "Validation error", SourceFile = "invalid.mdext", LineNumber = 1 }]
        };

        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((IEnumerable<MarkdownDocument>)[templateDoc1, templateDoc2, sourceDoc]));

        _compiler.Validate(Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(invalidResult);

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Return_Error_When_All_Files_Are_Invalid()
    {
        var templateDoc1 = new MarkdownDocument { FileName = "invalid1.mdext", Content = "Invalid content 1", FilePath = "invalid1.mdext" };
        var templateDoc2 = new MarkdownDocument { FileName = "invalid2.mdext", Content = "Invalid content 2", FilePath = "invalid2.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var invalidResult = new ValidationResult
        {
            Errors = [
                new ValidationIssue { Message = "Error 1", SourceFile = "invalid1.mdext", LineNumber = 1 },
                new ValidationIssue { Message = "Error 2", SourceFile = "invalid2.mdext", LineNumber = 1 }
            ]
        };

        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((IEnumerable<MarkdownDocument>)[templateDoc1, templateDoc2, sourceDoc]));

        _compiler.Validate(Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(invalidResult);

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "validate", null);
        var result = await _command.ExecuteAsync(context, settings);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Should_Call_Services_With_Correct_Parameters()
    {
        var templateDoc = new MarkdownDocument { FileName = "test.mdext", Content = "Test content", FilePath = "test.mdext" };
        var sourceDoc = new MarkdownDocument { FileName = "source.mdsrc", Content = "Source content", FilePath = "source.mdsrc" };

        var validResult = new ValidationResult();

        _fileSystemService.DirectoryExists(_testInputFolder).Returns(true);
        _collector.CollectAllMarkdownFilesAsync(_testInputFolder)
            .Returns(Task.FromResult((IEnumerable<MarkdownDocument>)[templateDoc, sourceDoc]));

        _compiler.Validate(Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(validResult);

        var settings = new ValidateCommand.Settings
        {
            InputFolder = _testInputFolder
        };

        var context = new CommandContext([], Substitute.For<IRemainingArguments>(), "validate", null);
        await _command.ExecuteAsync(context, settings);

        await Assert.MultipleAsync(async () =>
        {
            await _collector.Received(1).CollectAllMarkdownFilesAsync(_testInputFolder);
            _compiler.Received(1).Validate(Arg.Any<IEnumerable<MarkdownDocument>>());
        });
    }
}