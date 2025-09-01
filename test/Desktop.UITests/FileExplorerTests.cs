using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Desktop.ViewModels;
using Avalonia.VisualTree;

namespace Desktop.UITests;

[TestFixture]
public class FileExplorerTests : MainWindowTestBase
{
    [AvaloniaTest]
    public async Task FileExplorer_Should_Only_Expand_Root_Level_By_Default()
    {
        var fileExplorer = CreateFileExplorerWithNestedStructure();
        var window = new Window
        {
            Content = fileExplorer
        };

        window.Show();
        var viewModel = await SetupFileExplorerAndWaitForLoadAsync(fileExplorer);

        // Get the TreeView
        var treeView = fileExplorer.GetVisualDescendants().OfType<TreeView>().FirstOrDefault();
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
    public async Task FileExplorer_Should_Allow_Folder_Expansion_And_Load_Children()
    {
        var fileExplorer = CreateFileExplorerWithNestedStructure();
        var viewModel = await SetupFileExplorerAndWaitForLoadAsync(fileExplorer);

        // Get the src folder (should not be expanded initially)
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
        Assert.That(srcFolder!.IsExpanded, Is.False, "src folder should not be expanded initially");

        // Initially, src folder should have no children loaded but should indicate it has children
        Assert.That(srcFolder.Children.Count, Is.EqualTo(0), "src folder should have no children initially");
        Assert.That(srcFolder.HasChildren, Is.True, "src folder should indicate it has children");

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
    public async Task FileExplorer_Should_Allow_Multiple_Folder_Expansions()
    {
        var fileExplorer = CreateFileExplorerWithNestedStructure();
        var viewModel = await SetupFileExplorerAndWaitForLoadAsync(fileExplorer);

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
    public async Task FileExplorer_Should_Trigger_FileSelected_Event_When_File_Selected()
    {
        var fileExplorer = CreateFileExplorerWithNestedStructure();
        var viewModel = await SetupFileExplorerAndWaitForLoadAsync(fileExplorer);

        // Find the README.md file
        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        Assert.That(readmeFile, Is.Not.Null, "README.md file should exist");

        // Set up event handler to capture file selection
        string? selectedFilePath = null;
        viewModel.FileSelected += (sender, filePath) => selectedFilePath = filePath;

        // Select the file (simulating clicking in TreeView)
        readmeFile!.IsSelected = true;

        // Wait for event to be raised
        await WaitForConditionAsync(() => selectedFilePath != null, 1000);

        Assert.That(selectedFilePath, Is.EqualTo("/test/path/README.md"), "FileSelected event should be raised with correct file path");
    }

    [AvaloniaTest]
    public void FileExplorer_Should_Show_Loading_Indicator_Initially()
    {
        var fileExplorer = CreateFileExplorerWithNestedStructure();
        var window = new Window
        {
            Content = fileExplorer
        };

        window.Show();

        var viewModel = fileExplorer.DataContext as FileExplorerViewModel;
        Assert.That(viewModel, Is.Not.Null);

        // Initially should be loading
        Assert.That(viewModel!.IsLoading, Is.False, "Should not be loading initially");

        // Find loading indicator
        var loadingText = fileExplorer.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.Text == "Loading...");
        Assert.That(loadingText, Is.Not.Null, "Loading indicator should exist");
        Assert.That(loadingText!.IsVisible, Is.False, "Loading indicator should not be visible initially");

        // TreeView should be visible when not loading
        var treeView = fileExplorer.GetVisualDescendants().OfType<TreeView>().FirstOrDefault();
        Assert.That(treeView, Is.Not.Null, "TreeView should exist");
        Assert.That(treeView!.IsVisible, Is.True, "TreeView should be visible when not loading");
    }
}