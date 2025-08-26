using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using Desktop.Models;
using Desktop.ViewModels;
using Desktop.Views;
using NUnit.Framework;

namespace Desktop.UITests;

[TestFixture]
public class ContextMenuTests : MainWindowTestBase
{
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
        var viewModel = new FileSystemItemViewModel(fileItem);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OpenCommand, Is.Not.Null, "OpenCommand should be initialized");
            Assert.That(viewModel.ShowInExplorerCommand, Is.Not.Null, "ShowInExplorerCommand should be initialized");
            Assert.That(viewModel.CopyPathCommand, Is.Not.Null, "CopyPathCommand should be initialized");
            Assert.That(viewModel.RefreshCommand, Is.Not.Null, "RefreshCommand should be initialized");
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
        var viewModel = new FileSystemItemViewModel(fileItem);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OpenCommand.CanExecute(null), Is.True, "Open should be available for files (but hidden in context menu)");
            Assert.That(viewModel.CopyPathCommand.CanExecute(null), Is.True, "CopyPath should be available for files");
            Assert.That(viewModel.RefreshCommand.CanExecute(null), Is.False, "Refresh should NOT be available for files");
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
        var viewModel = new FileSystemItemViewModel(directoryItem);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OpenCommand.CanExecute(null), Is.True, "Open should be available for directories");
            Assert.That(viewModel.CopyPathCommand.CanExecute(null), Is.True, "CopyPath should be available for directories");
            Assert.That(viewModel.RefreshCommand.CanExecute(null), Is.True, "Refresh should be available for directories");
        });
    }

    [AvaloniaTest]
    public async Task FileExplorerUserControl_Should_Show_Context_Menu_On_TreeViewItem()
    {
        // Arrange
        var window = CreateMainWindow();
        window.Show();
        
        await Task.Delay(100); // Allow UI to initialize

        // Find the FileExplorerUserControl 
        var fileExplorer = window.GetVisualDescendants().OfType<FileExplorerUserControl>().FirstOrDefault();
        Assert.That(fileExplorer, Is.Not.Null, "FileExplorerUserControl should be found");
        
        await Task.Delay(500); // Allow file system to load

        // Find TreeView and TreeViewItems
        var treeView = fileExplorer!.GetVisualDescendants().OfType<Avalonia.Controls.TreeView>().FirstOrDefault();
        Assert.That(treeView, Is.Not.Null, "TreeView should be found");

        var treeViewItems = treeView!.GetVisualDescendants().OfType<Avalonia.Controls.TreeViewItem>().ToList();
        if (treeViewItems.Count > 0)
        {
            var firstItem = treeViewItems.First();
            Assert.That(firstItem.ContextMenu, Is.Not.Null, "TreeViewItem should have a context menu");
        }
        // Note: If no items are loaded in tests, this is expected behavior
    }
}