using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Desktop.Views;
using Desktop.ViewModels;
using Avalonia.VisualTree;
using System.Linq;

namespace Desktop.UITests;

[TestFixture]
public class FileExplorerTests : MainWindowTestBase
{
    [AvaloniaTest]
    public async Task MainWindow_Should_Only_Expand_Root_Level_By_Default()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Get the TreeView
        var treeView = window.GetVisualDescendants().OfType<TreeView>().FirstOrDefault();
        Assert.That(treeView, Is.Not.Null, "TreeView not found");

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");
        
        // Root should be expanded (auto-expanded)
        Assert.That(viewModel.RootItem!.IsExpanded, Is.True, "Root folder should be expanded by default");

        // Find child folders in the root
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        var testFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "test");

        Assert.Multiple(() =>
        {
            Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
            Assert.That(testFolder, Is.Not.Null, "test folder should exist");

            // Child folders should NOT be expanded by default
            Assert.That(srcFolder!.IsExpanded, Is.False, "src folder should NOT be expanded by default");
            Assert.That(testFolder!.IsExpanded, Is.False, "test folder should NOT be expanded by default");

            // Verify that child folders have children but they are not loaded yet (lazy loading)
            Assert.That(srcFolder.Item.HasChildren, Is.True, "src folder should have children in the model");
            Assert.That(testFolder.Item.HasChildren, Is.True, "test folder should have children in the model");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Allow_Folder_Expansion_And_Load_Children()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Get the src folder (should not be expanded initially)
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
        Assert.That(srcFolder!.IsExpanded, Is.False, "src folder should not be expanded initially");

        // Initially, src folder should have a placeholder child (Loading...)
        Assert.That(srcFolder.Children.Count, Is.EqualTo(1), "src folder should have placeholder child");
        Assert.That(srcFolder.Children[0].Name, Is.EqualTo("Loading..."), "Should have loading placeholder");

        // Expand the src folder and wait for children to load
        await ExpandFolderAndWaitAsync(srcFolder);

        // After expansion, children should be loaded
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.IsExpanded, Is.True, "src folder should be expanded");
            Assert.That(srcFolder.Children.Count, Is.EqualTo(3), "src folder should have 3 actual children");
            
            // Verify the actual children are loaded
            var componentsFolder = srcFolder.Children.FirstOrDefault(c => c.Name == "components");
            var utilsFolder = srcFolder.Children.FirstOrDefault(c => c.Name == "utils");
            var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

            Assert.That(componentsFolder, Is.Not.Null, "components folder should be loaded");
            Assert.That(utilsFolder, Is.Not.Null, "utils folder should be loaded");
            Assert.That(mainFile, Is.Not.Null, "main.cs file should be loaded");

            // Verify folder types
            Assert.That(componentsFolder!.IsDirectory, Is.True, "components should be a directory");
            Assert.That(utilsFolder!.IsDirectory, Is.True, "utils should be a directory");
            Assert.That(mainFile!.IsDirectory, Is.False, "main.cs should be a file");

            // Child folders should not be expanded by default
            Assert.That(componentsFolder.IsExpanded, Is.False, "components folder should not be expanded");
            Assert.That(utilsFolder.IsExpanded, Is.False, "utils folder should not be expanded");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Allow_Multiple_Folder_Expansions()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Get both src and test folders
        var srcFolder = viewModel.RootItem?.Children.FirstOrDefault(c => c.Name == "src");
        var testFolder = viewModel.RootItem?.Children.FirstOrDefault(c => c.Name == "test");

        Assert.Multiple(() =>
        {
            Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
            Assert.That(testFolder, Is.Not.Null, "test folder should exist");
        });

        // Expand both folders
        srcFolder!.IsExpanded = true;
        testFolder!.IsExpanded = true;

        // Wait for lazy loading to complete
        await WaitForConditionAsync(() => 
            srcFolder.Children.Any(c => c.Name != "Loading...") && 
            testFolder.Children.Any(c => c.Name != "Loading..."), 2000);

        Assert.Multiple(() =>
        {
            // Both should be expanded with proper children
            Assert.That(srcFolder.IsExpanded, Is.True, "src folder should be expanded");
            Assert.That(testFolder.IsExpanded, Is.True, "test folder should be expanded");
            
            Assert.That(srcFolder.Children.Count, Is.EqualTo(3), "src should have 3 children");
            Assert.That(testFolder.Children.Count, Is.EqualTo(2), "test should have 2 children");

            // Verify test folder contents
            var unitFolder = testFolder.Children.FirstOrDefault(c => c.Name == "unit");
            var integrationFolder = testFolder.Children.FirstOrDefault(c => c.Name == "integration");

            Assert.That(unitFolder, Is.Not.Null, "unit folder should be loaded");
            Assert.That(integrationFolder, Is.Not.Null, "integration folder should be loaded");
            Assert.That(unitFolder!.IsDirectory, Is.True, "unit should be a directory");
            Assert.That(integrationFolder!.IsDirectory, Is.True, "integration should be a directory");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Display_File_Content_In_Editor_When_File_Selected()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Expand the root to get the README.md file
        viewModel.RootItem!.IsExpanded = true;
        await WaitForConditionAsync(() => viewModel.RootItem.Children.Any());

        // Find the README.md file
        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        Assert.That(readmeFile, Is.Not.Null, "README.md file should exist");

        // Get the editor TextBox from the UI
        var editorTextBox = window.FindControl<TextBox>("DocumentEditor");
        Assert.That(editorTextBox, Is.Not.Null, "Document editor should exist");

        // Get the tab container from the UI
        var tabContainer = window.GetVisualDescendants().OfType<ItemsControl>()
            .FirstOrDefault(ic => ic.ItemsSource != null);
        Assert.That(tabContainer, Is.Not.Null, "Tab container should exist");

        // Initially no tabs should be open
        Assert.That(viewModel.EditorTabBar.EditorTabs.Count, Is.EqualTo(0), "No tabs should be open initially");
        Assert.That(editorTextBox.Text, Is.Null.Or.Empty, "Editor should be empty initially");

        // Select the file (simulating clicking in TreeView)
        readmeFile!.IsSelected = true;

        // Wait for file to load
        await WaitForConditionAsync(() => viewModel.EditorTabBar.EditorTabs.Count > 0, 1000);

        Assert.Multiple(() =>
        {
            // Verify tab was created and is visible
            Assert.That(viewModel.EditorTabBar.EditorTabs.Count, Is.EqualTo(1), "One tab should be open");
            
            var tab = viewModel.EditorTabBar.EditorTabs.First();
            Assert.That(tab.Title, Is.EqualTo("README.md"), "Tab title should be README.md");
            Assert.That(tab.FilePath, Is.EqualTo("/test/path/README.md"), "Tab file path should be correct");
            Assert.That(tab.IsActive, Is.True, "Tab should be active");
            Assert.That(tab.Content, Is.EqualTo("Mock file content"), "Tab should contain file content");
            
            // Verify active tab is set correctly
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(tab), "Active tab should be set");
            Assert.That(viewModel.EditorContent.ActiveFileContent, Is.EqualTo("Mock file content"), "Active file content should be available");
            
            // Verify editor displays the content
            Assert.That(editorTextBox.Text, Is.EqualTo("Mock file content"), "Editor should display file content");
            
            // Verify tab appears in UI (ItemsControl should have items)
            Assert.That(tabContainer.ItemCount, Is.EqualTo(1), "Tab should appear in UI");
        });
    }
}