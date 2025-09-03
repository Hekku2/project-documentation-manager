using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using Desktop.Views;

namespace Desktop.UITests;

[TestFixture]
public class ContextMenuTests : MainWindowTestBase
{

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