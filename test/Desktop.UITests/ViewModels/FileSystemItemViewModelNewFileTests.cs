using Avalonia.Headless.NUnit;
using Desktop.Factories;
using Desktop.Models;
using Desktop.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Desktop.UITests.ViewModels;

[TestFixture]
public class FileSystemItemViewModelNewFileTests
{
    private IFileSystemItemViewModelFactory _fileSystemItemViewModelFactory = null!;
    private IFileSystemExplorerService _fileSystemExplorerService = null!;
    private IFileService _fileService = null!;

    [SetUp]
    public void Setup()
    {
        var loggerFactory = NullLoggerFactory.Instance;
        _fileSystemExplorerService = Substitute.For<IFileSystemExplorerService>();
        _fileService = Substitute.For<IFileService>();
        
        _fileSystemItemViewModelFactory = new FileSystemItemViewModelFactory(
            loggerFactory,
            _fileSystemExplorerService,
            _fileService,
            _ => { }, // onItemSelected
            _ => { }  // onItemPreview
        );
    }

    [AvaloniaTest]
    public void NewCommand_WithDirectory_ShouldBeEnabled()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "TestFolder_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        
        try
        {
            var directoryItem = new FileSystemItem
            {
                Name = "TestFolder",
                FullPath = testPath,
                IsDirectory = true
            };

            // Act
            var viewModel = _fileSystemItemViewModelFactory.Create(directoryItem);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(viewModel.NewCommand, Is.Not.Null);
                Assert.That(viewModel.NewCommand.CanExecute(null), Is.True, "NewCommand should be enabled for directories");
            });
        }
        finally
        {
            if (Directory.Exists(testPath))
                Directory.Delete(testPath);
        }
    }

    [AvaloniaTest]
    public void NewCommand_WithFile_ShouldBeDisabled()
    {
        // Arrange
        var fileItem = new FileSystemItem
        {
            Name = "test.md",
            FullPath = "/test/path/test.md",
            IsDirectory = false
        };

        // Act
        var viewModel = _fileSystemItemViewModelFactory.Create(fileItem);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.NewCommand, Is.Not.Null);
            Assert.That(viewModel.NewCommand.CanExecute(null), Is.False, "NewCommand should be disabled for files");
        });
    }

    [AvaloniaTest]
    public async Task NewCommand_Execute_ShouldCallCreateFileAsync()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "TestFolder_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        
        try
        {
            var directoryItem = new FileSystemItem
            {
                Name = "TestFolder",
                FullPath = testPath,
                IsDirectory = true
            };

            _fileService.CreateFileAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
            _fileService.CreateFileSystemItem(Arg.Any<string>(), Arg.Any<bool>()).Returns(directoryItem);

            var viewModel = _fileSystemItemViewModelFactory.Create(directoryItem);

            // Act
            viewModel.NewCommand.Execute(null);
            
            // Wait a moment for async operation
            await Task.Delay(200);

            // Assert
            await _fileService.Received(1).CreateFileAsync(testPath, "newfile.md");
        }
        finally
        {
            if (Directory.Exists(testPath))
                Directory.Delete(testPath);
        }
    }

    [AvaloniaTest]
    public async Task NewCommand_WhenCreateFileSucceeds_ShouldRefreshFolder()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "TestFolder_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        
        try
        {
            var directoryItem = new FileSystemItem
            {
                Name = "TestFolder",
                FullPath = testPath,
                IsDirectory = true
            };

            var refreshedItem = new FileSystemItem
            {
                Name = "TestFolder", 
                FullPath = testPath,
                IsDirectory = true
            };
            refreshedItem.Children.Add(new FileSystemItem
            {
                Name = "newfile.md",
                FullPath = Path.Combine(testPath, "newfile.md"),
                IsDirectory = false
            });

            _fileService.CreateFileAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
            _fileService.CreateFileSystemItem(testPath, true).Returns(refreshedItem);

            var viewModel = _fileSystemItemViewModelFactory.CreateWithChildren(directoryItem);
            
            // Simulate that the folder is visible and expanded so refresh will work
            viewModel.IsVisible = true;
            viewModel.IsExpanded = true;
            
            // Wait for initial loading
            await Task.Delay(100);

            // Act
            viewModel.NewCommand.Execute(null);
            
            // Wait for async operations
            await Task.Delay(500);

            // Assert
            await _fileService.Received(1).CreateFileAsync(testPath, "newfile.md");
            _fileService.Received(1).CreateFileSystemItem(testPath, true);
            
            Assert.Multiple(() =>
            {
                Assert.That(directoryItem.Children, Has.Count.EqualTo(1), "Directory should have refreshed children");
                Assert.That(directoryItem.Children[0].Name, Is.EqualTo("newfile.md"), "New file should be in children");
            });
        }
        finally
        {
            if (Directory.Exists(testPath))
                Directory.Delete(testPath, true);
        }
    }
}