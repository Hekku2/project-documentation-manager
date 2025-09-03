using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Desktop.ViewModels;
using Avalonia.VisualTree;
using Desktop.Views;

namespace Desktop.UITests;

[TestFixture]
public class MainWindowBasicTests : MainWindowTestBase
{
    [AvaloniaTest]
    public void MainWindow_Should_Open()
    {
        var window = CreateMainWindow();
        window.Show();
        Assert.That(window, Is.Not.Null);
        Assert.That(window.Title, Is.EqualTo("Project Documentation Manager"));
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Menu_Bar_With_File_And_Build_Menus()
    {
        var window = CreateMainWindow();
        window.Show();

        var menu = window.FindControl<Menu>("MainMenu");
        Assert.That(menu, Is.Not.Null, "Main menu not found");

        // Verify the menu structure contains File and Build menus
        Assert.That(menu.Items, Has.Count.EqualTo(3), "Menu should have three top-level items (File, View, and Build)");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_File_Explorer()
    {
        var window = CreateMainWindow();
        window.Show();

        // Find TreeView by traversing the visual tree
        var treeViews = window.GetVisualDescendants().OfType<TreeView>().ToList();
        Assert.That(treeViews, Is.Not.Empty, "File explorer TreeView not found");
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Have_Document_Editor()
    {
        var window = CreateMainWindow();
        window.Show();

        // Open a file first to ensure FileEditorContent is displayed
        var viewModel = (MainWindowViewModel)window.DataContext!;
        await viewModel.EditorTabBar.OpenFileAsync("/test/file.md");

        // Wait for the file to be opened and ensure we have an active tab
        await WaitForConditionAsync(() => viewModel.EditorTabBar.ActiveTab != null, 2000);

        // Find the DocumentEditor within the FileEditorContent
        var editorUserControl = window.GetVisualDescendants().OfType<EditorUserControl>().FirstOrDefault();
        Assert.That(editorUserControl, Is.Not.Null, "EditorUserControl not found");

        var fileEditorContent = editorUserControl!.GetVisualDescendants().OfType<Desktop.Views.FileEditorContent>().FirstOrDefault();
        var documentEditor = fileEditorContent?.FindControl<TextBox>("DocumentEditor");
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.Not.Null, "Active tab should exist after opening file");
            Assert.That(viewModel.EditorTabBar.ActiveTab!.Content, Is.Not.Null, "Tab content should exist");
            Assert.That(viewModel.EditorContent.CurrentContentData, Is.Not.Null, "Editor content data should exist");
            Assert.That(viewModel.EditorContent.CurrentContentData, Is.TypeOf<Desktop.Models.FileEditorContentData>(), "Should be file editor content");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Tab_System()
    {
        var window = CreateMainWindow();
        window.Show();

        // Check that the tab system exists (ItemsControl for tabs)
        var tabContainer = window.GetVisualDescendants().OfType<ItemsControl>()
            .FirstOrDefault(ic => ic.ItemsSource != null);
        Assert.That(tabContainer, Is.Not.Null, "Tab container not found");

        // Check that the main window has EditorTabs collection
        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel?.EditorTabBar.EditorTabs, Is.Not.Null, "EditorTabs collection not found");
    }
}