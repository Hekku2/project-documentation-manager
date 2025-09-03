using Avalonia.Headless.NUnit;
using Desktop.Factories;
using Desktop.Models;
using Desktop.Services;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Desktop.UITests.ViewModels;

[TestFixture]
public class FileSystemItemViewModelTests
{
    private IFileSystemItemViewModelFactory _fileSystemItemViewModelFactory = null!;
    private IFileSystemExplorerService _fileSystemExplorerService = null!;
    private IFileSystemChangeHandler _fileSystemChangeHandler = null!;

    [SetUp]
    public void Setup()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger<FileSystemItemViewModel>().Returns(Substitute.For<ILogger<FileSystemItemViewModel>>());
        
        _fileSystemExplorerService = Substitute.For<IFileSystemExplorerService>();
        _fileSystemChangeHandler = Substitute.For<IFileSystemChangeHandler>();
        
        _fileSystemItemViewModelFactory = new FileSystemItemViewModelFactory(
            loggerFactory,
            _fileSystemExplorerService,
            _fileSystemChangeHandler,
            _ => { }, // onItemSelected
            _ => { }  // onItemPreview
        );
    }

    [AvaloniaTest]
    public void FileSystemItemViewModel_Should_Have_Context_Menu_Commands()
    {
        // Arrange
        var fileItem = new FileSystemItem
        {
            Name = "test.txt",
            FullPath = "/test/path/test.txt",
            IsDirectory = false
        };

        // Act
        var viewModel = _fileSystemItemViewModelFactory.Create(fileItem);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OpenCommand, Is.Not.Null, "OpenCommand should be initialized");
            Assert.That(viewModel.ShowInExplorerCommand, Is.Not.Null, "ShowInExplorerCommand should be initialized");
            Assert.That(viewModel.CopyPathCommand, Is.Not.Null, "CopyPathCommand should be initialized");
            Assert.That(viewModel.RefreshCommand, Is.Not.Null, "RefreshCommand should be initialized");
            Assert.That(viewModel.ShowInPreviewCommand, Is.Not.Null, "ShowInPreviewCommand should be initialized");
        });
    }

    [AvaloniaTest]
    public void FileSystemItemViewModel_Commands_Should_Have_Correct_CanExecute_For_Files()
    {
        // Arrange
        var fileItem = new FileSystemItem
        {
            Name = "test.txt",
            FullPath = "/test/path/test.txt",
            IsDirectory = false
        };
        var viewModel = _fileSystemItemViewModelFactory.Create(fileItem);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OpenCommand.CanExecute(null), Is.True, "Open should be available for files (but hidden in context menu)");
            Assert.That(viewModel.CopyPathCommand.CanExecute(null), Is.True, "CopyPath should be available for files");
            Assert.That(viewModel.RefreshCommand.CanExecute(null), Is.False, "Refresh should NOT be available for files");
            Assert.That(viewModel.ShowInPreviewCommand.CanExecute(null), Is.False, "ShowInPreview should NOT be available for regular files");
        });
    }

    [AvaloniaTest]
    public void FileSystemItemViewModel_Commands_Should_Have_Correct_CanExecute_For_Directories()
    {
        // Arrange
        var directoryItem = new FileSystemItem
        {
            Name = "TestFolder",
            FullPath = "/test/path/TestFolder",
            IsDirectory = true
        };
        var viewModel = _fileSystemItemViewModelFactory.Create(directoryItem);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OpenCommand.CanExecute(null), Is.True, "Open should be available for directories");
            Assert.That(viewModel.CopyPathCommand.CanExecute(null), Is.True, "CopyPath should be available for directories");
            Assert.That(viewModel.RefreshCommand.CanExecute(null), Is.True, "Refresh should be available for directories");
            Assert.That(viewModel.ShowInPreviewCommand.CanExecute(null), Is.False, "ShowInPreview should NOT be available for directories");
        });
    }

    [AvaloniaTest]
    public void FileSystemItemViewModel_Commands_Should_Have_Correct_CanExecute_For_MarkdownTemplates()
    {
        // Arrange - Test .mdext file
        var mdextItem = new FileSystemItem
        {
            Name = "template.mdext",
            FullPath = "/test/path/template.mdext",
            IsDirectory = false
        };
        var mdextViewModel = _fileSystemItemViewModelFactory.Create(mdextItem);

        // Arrange - Test .mdsrc file
        var mdsrcItem = new FileSystemItem
        {
            Name = "source.mdsrc",
            FullPath = "/test/path/source.mdsrc",
            IsDirectory = false
        };
        var mdsrcViewModel = _fileSystemItemViewModelFactory.Create(mdsrcItem);

        // Arrange - Test .md file
        var mdItem = new FileSystemItem
        {
            Name = "readme.md",
            FullPath = "/test/path/readme.md",
            IsDirectory = false
        };
        var mdViewModel = _fileSystemItemViewModelFactory.Create(mdItem);

        // Assert
        Assert.Multiple(() =>
        {
            // Test .mdext file
            Assert.That(mdextViewModel.IsMarkdownTemplate, Is.True, ".mdext file should be identified as markdown template");
            Assert.That(mdextViewModel.ShowInPreviewCommand.CanExecute(null), Is.True, "ShowInPreview should be available for .mdext files");
            
            // Test .mdsrc file
            Assert.That(mdsrcViewModel.IsMarkdownTemplate, Is.True, ".mdsrc file should be identified as markdown template");
            Assert.That(mdsrcViewModel.ShowInPreviewCommand.CanExecute(null), Is.True, "ShowInPreview should be available for .mdsrc files");
            
            // Test .md file
            Assert.That(mdViewModel.IsMarkdownTemplate, Is.True, ".md file should be identified as markdown template");
            Assert.That(mdViewModel.ShowInPreviewCommand.CanExecute(null), Is.True, "ShowInPreview should be available for .md files");
            
            // Verify other commands work normally
            Assert.That(mdextViewModel.OpenCommand.CanExecute(null), Is.True, "Open should be available for markdown template files");
            Assert.That(mdextViewModel.CopyPathCommand.CanExecute(null), Is.True, "CopyPath should be available for markdown template files");
            Assert.That(mdextViewModel.RefreshCommand.CanExecute(null), Is.False, "Refresh should NOT be available for files");
        });
    }
}