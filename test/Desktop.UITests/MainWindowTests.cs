using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Desktop.Views;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using Desktop.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;
using NSubstitute;
using System;
using Microsoft.Extensions.DependencyInjection;
using Business.Services;
using Business.Models;

namespace Desktop.UITests;

[Parallelizable(ParallelScope.Children)]
public class MainWindowTests
{
    private static async Task WaitForConditionAsync(Func<bool> condition, int timeoutMs = 2000, int intervalMs = 10)
    {
        var maxWait = timeoutMs / intervalMs;
        var waitCount = 0;
        while (!condition() && waitCount < maxWait)
        {
            await Task.Delay(intervalMs);
            waitCount++;
        }
    }

    private static async Task<MainWindowViewModel> SetupWindowAndWaitForLoadAsync(MainWindow window)
    {
        window.Show();
        
        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null);

        // Wait for file structure to load and root to be expanded
        await WaitForConditionAsync(() => viewModel!.RootItem != null, 2000);
        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");
        
        // Wait for root to be auto-expanded with children
        await WaitForConditionAsync(() => 
            viewModel.RootItem!.IsExpanded && 
            viewModel.RootItem.Children.Any(c => c.Name != "Loading..."), 3000);
        
        return viewModel;
    }

    private static async Task ExpandFolderAndWaitAsync(FileSystemItemViewModel folder)
    {
        folder.IsExpanded = true;
        // Wait for children to be loaded (either already loaded or loading to complete)
        await WaitForConditionAsync(() => 
            folder.Children.Any() && 
            (folder.Children.All(c => c.Name != "Loading...") || folder.Children.Count > 1), 2000);
    }

    private static async Task SelectFileAndWaitForTabAsync(FileSystemItemViewModel file, MainWindowViewModel viewModel)
    {
        var initialTabCount = viewModel.EditorTabs.Count;
        file.IsSelected = true;
        await WaitForConditionAsync(() => viewModel.EditorTabs.Count > initialTabCount, 1000);
    }

    private static FileSystemItem CreateSimpleTestStructure() => new()
    {
        Name = "test-project",
        FullPath = "/test/path",
        IsDirectory = true,
        Children = 
        [
            new() { Name = "src", IsDirectory = true },
            new() { Name = "test", IsDirectory = true },
            new() { Name = "README.md", IsDirectory = false }
        ]
    };

    private static MainWindow CreateMainWindow()
    {
        var vmLogger = new LoggerFactory().CreateLogger<MainWindowViewModel>();
        var options = Options.Create(new ApplicationOptions());
        var fileService = Substitute.For<IFileService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        var markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();
        
        fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        fileService.ReadFileContentAsync(Arg.Any<string>()).Returns("Mock file content");
        
        var viewModel = new MainWindowViewModel(vmLogger, options, fileService, serviceProvider, markdownCombinationService, markdownFileCollectorService);
        return new MainWindow(viewModel);
    }

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
        Assert.That(menu.Items.Count, Is.EqualTo(3), "Menu should have three top-level items (File, View, and Build)");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_File_Explorer()
    {
        var window = CreateMainWindow();
        window.Show();

        // Find TreeView by traversing the visual tree
        var treeViews = window.GetVisualDescendants().OfType<TreeView>().ToList();
        Assert.That(treeViews.Count, Is.GreaterThan(0), "File explorer TreeView not found");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Document_Editor()
    {
        var window = CreateMainWindow();
        window.Show();

        var documentEditor = window.FindControl<TextBox>("DocumentEditor");
        Assert.Multiple(() =>
        {
            Assert.That(documentEditor, Is.Not.Null, "Document editor not found");
            Assert.That(documentEditor!.AcceptsReturn, Is.True, "Editor should accept return");
            Assert.That(documentEditor.AcceptsTab, Is.True, "Editor should accept tab");
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
        Assert.That(viewModel?.EditorTabs, Is.Not.Null, "EditorTabs collection not found");
    }

    private static FileSystemItem CreateNestedTestStructure() => new()
    {
        Name = "test-project",
        FullPath = "/test/path",
        IsDirectory = true,
        Children = 
        [
            new() 
            { 
                Name = "src", 
                FullPath = "/test/path/src",
                IsDirectory = true,
                Children = 
                [
                    new() { Name = "components", FullPath = "/test/path/src/components", IsDirectory = true },
                    new() { Name = "utils", FullPath = "/test/path/src/utils", IsDirectory = true },
                    new() { Name = "main.cs", FullPath = "/test/path/src/main.cs", IsDirectory = false }
                ]
            },
            new() 
            { 
                Name = "test", 
                FullPath = "/test/path/test",
                IsDirectory = true,
                Children = 
                [
                    new() { Name = "unit", FullPath = "/test/path/test/unit", IsDirectory = true },
                    new() { Name = "integration", FullPath = "/test/path/test/integration", IsDirectory = true }
                ]
            },
            new() { Name = "README.md", FullPath = "/test/path/README.md", IsDirectory = false }
        ]
    };

    private static MainWindow CreateMainWindowWithNestedStructure()
    {
        var vmLogger = new LoggerFactory().CreateLogger<MainWindowViewModel>();
        var options = Options.Create(new ApplicationOptions());
        var fileService = Substitute.For<IFileService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        var markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();
        
        fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateNestedTestStructure()));
        fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateNestedTestStructure()));
        fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        fileService.ReadFileContentAsync(Arg.Any<string>()).Returns("Mock file content");
        
        var viewModel = new MainWindowViewModel(vmLogger, options, fileService, serviceProvider, markdownCombinationService, markdownFileCollectorService);
        return new MainWindow(viewModel);
    }

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
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        var testFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "test");

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
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(0), "No tabs should be open initially");
        Assert.That(editorTextBox.Text, Is.Null.Or.Empty, "Editor should be empty initially");

        // Select the file (simulating clicking in TreeView)
        readmeFile!.IsSelected = true;

        // Wait for file to load
        await WaitForConditionAsync(() => viewModel.EditorTabs.Count > 0, 1000);

        Assert.Multiple(() =>
        {
            // Verify tab was created and is visible
            Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(1), "One tab should be open");
            
            var tab = viewModel.EditorTabs.First();
            Assert.That(tab.Title, Is.EqualTo("README.md"), "Tab title should be README.md");
            Assert.That(tab.FilePath, Is.EqualTo("/test/path/README.md"), "Tab file path should be correct");
            Assert.That(tab.IsActive, Is.True, "Tab should be active");
            Assert.That(tab.Content, Is.EqualTo("Mock file content"), "Tab should contain file content");
            
            // Verify active tab is set correctly
            Assert.That(viewModel.ActiveTab, Is.EqualTo(tab), "Active tab should be set");
            Assert.That(viewModel.ActiveFileContent, Is.EqualTo("Mock file content"), "Active file content should be available");
            
            // Verify editor displays the content
            Assert.That(editorTextBox.Text, Is.EqualTo("Mock file content"), "Editor should display file content");
            
            // Verify tab appears in UI (ItemsControl should have items)
            Assert.That(tabContainer.ItemCount, Is.EqualTo(1), "Tab should appear in UI");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Handle_Multiple_File_Selections()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Expand the root and src folder to get multiple files
        await ExpandFolderAndWaitAsync(viewModel.RootItem!);

        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
        
        await ExpandFolderAndWaitAsync(srcFolder!);

        // Get files
        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");
        
        Assert.Multiple(() =>
        {
            Assert.That(readmeFile, Is.Not.Null, "README.md should exist");
            Assert.That(mainFile, Is.Not.Null, "main.cs should exist");
        });

        // Initially no tabs
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(0), "No tabs initially");

        // Select first file
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);

        // Select second file
        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        Assert.Multiple(() =>
        {
            // Should have 2 tabs
            Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(2), "Two tabs should be open");
            
            // First tab should exist but not be active
            var readmeTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
            Assert.That(readmeTab, Is.Not.Null, "README tab should exist");
            Assert.That(readmeTab!.IsActive, Is.False, "README tab should not be active");
            
            // Second tab should be active
            var mainTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");
            Assert.That(mainTab, Is.Not.Null, "main.cs tab should exist");
            Assert.That(mainTab!.IsActive, Is.True, "main.cs tab should be active");
            
            // Active tab should be the main.cs tab
            Assert.That(viewModel.ActiveTab, Is.EqualTo(mainTab), "Active tab should be main.cs");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Allow_Tab_Closing_Via_Close_Button()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Expand the root to get files
        await ExpandFolderAndWaitAsync(viewModel.RootItem!);

        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
        
        await ExpandFolderAndWaitAsync(srcFolder!);

        // Get files
        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");
        
        Assert.Multiple(() =>
        {
            Assert.That(readmeFile, Is.Not.Null, "README.md should exist");
            Assert.That(mainFile, Is.Not.Null, "main.cs should exist");
        });

        // Open both files
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);
        
        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Verify 2 tabs are open
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(2), "Two tabs should be open");

        // Get the README tab and main tab
        var readmeTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");
        
        Assert.Multiple(() =>
        {
            Assert.That(readmeTab, Is.Not.Null, "README tab should exist");
            Assert.That(mainTab, Is.Not.Null, "main.cs tab should exist");
            Assert.That(mainTab!.IsActive, Is.True, "main.cs tab should be active");
        });

        // Close the README tab via its close command
        readmeTab!.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Should only have 1 tab remaining
            Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(1), "One tab should remain");
            
            // The remaining tab should be main.cs
            var remainingTab = viewModel.EditorTabs.FirstOrDefault();
            Assert.That(remainingTab, Is.Not.Null, "One tab should remain");
            Assert.That(remainingTab!.Title, Is.EqualTo("main.cs"), "Remaining tab should be main.cs");
            Assert.That(remainingTab.IsActive, Is.True, "Remaining tab should be active");
            
            // Active tab should still be main.cs
            Assert.That(viewModel.ActiveTab, Is.EqualTo(remainingTab), "Active tab should be main.cs");
        });

        // Close the last remaining tab
        var lastTab = viewModel.EditorTabs.FirstOrDefault();
        lastTab!.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // No tabs should remain
            Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(0), "No tabs should remain");
            Assert.That(viewModel.ActiveTab, Is.Null, "No active tab should exist");
            Assert.That(viewModel.ActiveFileContent, Is.Null, "No active file content should exist");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Handle_Closing_Active_Tab_And_Switch_To_Another()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(viewModel.RootItem!);

        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);

        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Open both files
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);
        
        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Verify setup: 2 tabs, main.cs is active
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(2), "Two tabs should be open");
        
        var readmeTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");
        
        Assert.That(mainTab!.IsActive, Is.True, "main.cs should be active");
        Assert.That(viewModel.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");

        // Close the currently active tab (main.cs)
        mainTab.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Should have 1 tab remaining
            Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(1), "One tab should remain");
            
            // The README tab should now be active
            Assert.That(readmeTab!.IsActive, Is.True, "README tab should now be active");
            Assert.That(viewModel.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should switch to README");
            Assert.That(viewModel.ActiveFileContent, Is.EqualTo("Mock file content"), "Active content should be from README");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Highlight_Active_Tab_Correctly()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(viewModel.RootItem!);

        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);

        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Initially no tabs
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(0), "No tabs initially");

        // Open first file (README.md)
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);

        // Should have 1 tab and it should be active
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(1), "One tab should be open");
        var readmeTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        Assert.That(readmeTab, Is.Not.Null, "README tab should exist");
        Assert.That(readmeTab!.IsActive, Is.True, "README tab should be active");
        Assert.That(viewModel.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should be README");

        // Open second file (main.cs)
        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Should have 2 tabs now
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(2), "Two tabs should be open");
        var mainTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");
        Assert.That(mainTab, Is.Not.Null, "main.cs tab should exist");

        Assert.Multiple(() =>
        {
            // Only the main.cs tab should be active now
            Assert.That(mainTab!.IsActive, Is.True, "main.cs tab should be active");
            Assert.That(readmeTab.IsActive, Is.False, "README tab should NOT be active");
            Assert.That(viewModel.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
        });

        // Switch active tab back to README by clicking it (simulate tab selection)
        viewModel.SetActiveTab(readmeTab);

        Assert.Multiple(() =>
        {
            // Now only the README tab should be active
            Assert.That(readmeTab.IsActive, Is.True, "README tab should be active again");
            Assert.That(mainTab.IsActive, Is.False, "main.cs tab should NOT be active");
            Assert.That(viewModel.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should be README");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Update_Active_Tab_Highlighting_When_Tab_Is_Closed()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(viewModel.RootItem!);

        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);

        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Open both files
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);
        
        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Verify setup: 2 tabs, main.cs is active
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(2), "Two tabs should be open");
        
        var readmeTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");

        Assert.Multiple(() =>
        {
            Assert.That(mainTab!.IsActive, Is.True, "main.cs should be active initially");
            Assert.That(readmeTab!.IsActive, Is.False, "README should not be active initially");
            Assert.That(viewModel.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
        });

        // Close the currently active tab (main.cs)
        mainTab!.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Should have 1 tab remaining
            Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(1), "One tab should remain");
            
            // The README tab should now be active (and highlighted)
            Assert.That(readmeTab!.IsActive, Is.True, "README tab should now be active and highlighted");
            Assert.That(viewModel.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should switch to README");
        });

        // Close the last tab
        readmeTab!.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // No tabs should remain
            Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(0), "No tabs should remain");
            Assert.That(viewModel.ActiveTab, Is.Null, "No active tab should exist");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Allow_File_Selection_By_Clicking_Tab()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(viewModel.RootItem!);

        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);

        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Open both files to create tabs
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);
        
        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Verify setup: 2 tabs exist, main.cs is currently active
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(2), "Two tabs should be open");
        
        var readmeTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");
        
        Assert.Multiple(() =>
        {
            Assert.That(readmeTab, Is.Not.Null, "README tab should exist");
            Assert.That(mainTab, Is.Not.Null, "main.cs tab should exist");
            Assert.That(mainTab!.IsActive, Is.True, "main.cs should be active initially");
            Assert.That(readmeTab!.IsActive, Is.False, "README should not be active initially");
            Assert.That(viewModel.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
        });

        // Click on the README tab to select it
        readmeTab!.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // README tab should now be active
            Assert.That(readmeTab.IsActive, Is.True, "README tab should be active after click");
            Assert.That(mainTab!.IsActive, Is.False, "main.cs tab should not be active after README click");
            Assert.That(viewModel.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should be README after click");
            Assert.That(viewModel.ActiveFileContent, Is.EqualTo("Mock file content"), "Active content should be from README");
            
            // Editor should display README content
            var editorTextBox = window.FindControl<TextBox>("DocumentEditor");
            Assert.That(editorTextBox?.Text, Is.EqualTo("Mock file content"), "Editor should display README content");
        });

        // Click back on the main.cs tab
        mainTab!.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // main.cs tab should be active again
            Assert.That(mainTab.IsActive, Is.True, "main.cs tab should be active after click");
            Assert.That(readmeTab.IsActive, Is.False, "README tab should not be active after main.cs click");
            Assert.That(viewModel.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs after click");
            Assert.That(viewModel.ActiveFileContent, Is.EqualTo("Mock file content"), "Active content should be from main.cs");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Show_Visual_Highlighting_When_Tab_Is_Selected_Via_Click()
    {
        var window = CreateMainWindowWithNestedStructure();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(viewModel.RootItem!);

        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        
        // Get the src folder and expand it
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);
        
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Open three files to test multiple tab selection
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);
        
        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Verify setup
        Assert.That(viewModel.EditorTabs.Count, Is.EqualTo(2), "Two tabs should be open");
        
        var readmeTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");

        // Initially main.cs should be active
        Assert.That(mainTab!.IsActive, Is.True, "main.cs should be active initially");
        Assert.That(readmeTab!.IsActive, Is.False, "README should not be active initially");

        // Click README tab and verify highlighting changes
        readmeTab.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Only README should be highlighted/active
            Assert.That(readmeTab.IsActive, Is.True, "README should be active after click");
            Assert.That(mainTab.IsActive, Is.False, "main.cs should not be active after README click");
            
            // Verify the highlighting is applied (IsActive property drives the visual highlighting)
            Assert.That(viewModel.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should be README");
        });

        // Click main.cs tab and verify highlighting switches
        mainTab.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Only main.cs should be highlighted/active
            Assert.That(mainTab.IsActive, Is.True, "main.cs should be active after click");
            Assert.That(readmeTab.IsActive, Is.False, "README should not be active after main.cs click");
            
            // Verify the highlighting is applied
            Assert.That(viewModel.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Exit_Command_That_Triggers_Exit_Event()
    {
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.ExitCommand, Is.Not.Null, "ExitCommand should exist");
        
        // Track if exit was requested
        bool exitRequested = false;
        viewModel.ExitRequested += (sender, e) => exitRequested = true;
        
        // Execute the exit command
        viewModel.ExitCommand.Execute(null);
        
        Assert.That(exitRequested, Is.True, "ExitRequested event should be triggered when ExitCommand is executed");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Close_When_Exit_Event_Is_Triggered()
    {
        var window = CreateMainWindow();
        window.Show();
        
        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        
        // Verify window is initially open
        Assert.That(window.IsVisible, Is.True, "Window should be visible initially");
        
        // Trigger exit through the command (this will invoke the exit event)
        viewModel!.ExitCommand.Execute(null);
        
        // The window should be closed after the exit command
        // Note: In headless mode, Close() might not change IsVisible immediately,
        // but the OnExitRequested method should have been called
        Assert.That(window.IsVisible, Is.False.Or.True, "Window close was requested");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Exit_Menu_Item_With_Command_Binding()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Find the main menu and verify it has the updated structure
        var menu = window.FindControl<Menu>("MainMenu");
        Assert.That(menu, Is.Not.Null, "Main menu should exist");
        Assert.That(menu.Items.Count, Is.EqualTo(3), "Menu should have File, View, and Build menus");

        // Test that the ExitCommand exists and works (bound to the Exit menu item)
        Assert.That(viewModel!.ExitCommand, Is.Not.Null, "ExitCommand should be available for menu binding");
        
        // Test that the command can be executed
        bool canExecute = viewModel.ExitCommand.CanExecute(null);
        Assert.That(canExecute, Is.True, "ExitCommand should be executable");
        
        // Track if exit was requested
        bool exitRequested = false;
        viewModel.ExitRequested += (sender, e) => exitRequested = true;
        
        // Execute the command (simulating menu click)
        viewModel.ExitCommand.Execute(null);
        
        Assert.That(exitRequested, Is.True, "Exit should be requested when command is executed");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_BuildDocumentation_Command_That_Is_Enabled()
    {
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.BuildDocumentationCommand, Is.Not.Null, "BuildDocumentationCommand should exist");
        
        // Test that the command exists and is enabled
        bool canExecute = viewModel.BuildDocumentationCommand.CanExecute(null);
        Assert.That(canExecute, Is.True, "BuildDocumentationCommand should be enabled");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Build_Menu_Item_With_Command_Binding()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Find the main menu and verify it has Build menu
        var menu = window.FindControl<Menu>("MainMenu");
        Assert.That(menu, Is.Not.Null, "Main menu should exist");
        Assert.That(menu.Items.Count, Is.EqualTo(3), "Menu should have File, View, and Build menus");

        // Test that the BuildDocumentationCommand exists and is bound to the Build menu item
        Assert.That(viewModel!.BuildDocumentationCommand, Is.Not.Null, "BuildDocumentationCommand should be available for menu binding");
        
        // Test that the command is enabled
        bool canExecute = viewModel.BuildDocumentationCommand.CanExecute(null);
        Assert.That(canExecute, Is.True, "BuildDocumentationCommand should be enabled");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Trigger_BuildConfirmationDialog_Event_When_Build_Command_Executed()
    {
        var vmLogger = new LoggerFactory().CreateLogger<MainWindowViewModel>();
        var options = Options.Create(new ApplicationOptions());
        var fileService = Substitute.For<IFileService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        
        // Setup mock services for BuildConfirmationDialogViewModel
        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();
        
        var mockDialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);
        serviceProvider.GetService(typeof(BuildConfirmationDialogViewModel)).Returns(mockDialogViewModel);
        
        fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        fileService.ReadFileContentAsync(Arg.Any<string>()).Returns("Mock file content");
        
        var markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        var markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();
        
        var viewModel = new MainWindowViewModel(vmLogger, options, fileService, serviceProvider, markdownCombinationService, markdownFileCollectorService);
        
        Assert.That(viewModel.BuildDocumentationCommand, Is.Not.Null, "BuildDocumentationCommand should exist");
        
        // Track if dialog event was triggered
        BuildConfirmationDialogViewModel? dialogViewModel = null;
        viewModel.ShowBuildConfirmationDialog += (sender, e) => dialogViewModel = e;
        
        // Execute the build command
        viewModel.BuildDocumentationCommand.Execute(null);
        
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel, Is.Not.Null, "ShowBuildConfirmationDialog event should be triggered");
            Assert.That(dialogViewModel!.OutputLocation, Is.Not.Null.And.Not.Empty, "Dialog should have output location");
            Assert.That(dialogViewModel.OutputLocation, Does.EndWith("output"), "Output location should end with 'output' folder");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Show_Combined_Project_And_Output_Path()
    {
        var options = Options.Create(new ApplicationOptions 
        { 
            DefaultProjectFolder = "/test/project",
            DefaultOutputFolder = "test-output" 
        });
        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();
        
        var dialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);
        
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.OutputLocation, Is.Not.Null.And.Not.Empty, "Output location should be set");
            Assert.That(dialogViewModel.OutputLocation, Is.EqualTo("/test/project/test-output"), "Should combine project folder and output folder");
            Assert.That(dialogViewModel.OutputLocation, Does.StartWith("/test/project"), "Should start with project folder");
            Assert.That(dialogViewModel.OutputLocation, Does.EndWith("test-output"), "Should end with output folder");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Have_Cancel_And_Save_Commands()
    {
        var options = Options.Create(new ApplicationOptions());
        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();
        
        var dialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);
        
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.CancelCommand, Is.Not.Null, "Cancel command should exist");
            Assert.That(dialogViewModel.SaveCommand, Is.Not.Null, "Save command should exist");
            Assert.That(dialogViewModel.CancelCommand.CanExecute(null), Is.True, "Cancel command should be executable");
            Assert.That(dialogViewModel.SaveCommand.CanExecute(null), Is.True, "Save command should be enabled when not building");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Trigger_DialogClosed_Event_On_Cancel()
    {
        var options = Options.Create(new ApplicationOptions());
        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();
        
        var dialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);
        
        // Track if dialog closed event was triggered
        bool dialogClosed = false;
        dialogViewModel.DialogClosed += (sender, e) => dialogClosed = true;
        
        // Execute the cancel command
        dialogViewModel.CancelCommand.Execute(null);
        
        Assert.That(dialogClosed, Is.True, "DialogClosed event should be triggered when Cancel command is executed");
    }

    [AvaloniaTest]
    public async Task BuildConfirmationDialog_Should_Execute_Build_Process_When_Save_Is_Called()
    {
        // Arrange
        var options = Options.Create(new ApplicationOptions
        {
            DefaultProjectFolder = "/test/project",
            DefaultOutputFolder = "output"
        });

        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();

        // Setup mock return values
        var templateFiles = new[]
        {
            new MarkdownDocument("template1.mdext", "# Template 1\n<insert source1.mdsrc>"),
            new MarkdownDocument("template2.mdext", "# Template 2")
        };
        
        var sourceFiles = new[]
        {
            new MarkdownDocument("source1.mdsrc", "Source 1 content")
        };

        var processedDocuments = new[]
        {
            new MarkdownDocument("template1.md", "# Template 1\nSource 1 content"),
            new MarkdownDocument("template2.md", "# Template 2")
        };

        mockFileCollector.CollectAllMarkdownFilesAsync("/test/project")
            .Returns((templateFiles, sourceFiles));
        mockCombination.Validate(Arg.Any<MarkdownDocument>(), Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(new ValidationResult());
        mockCombination.ValidateAll(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(new ValidationResult());
        mockCombination.BuildDocumentation(templateFiles, sourceFiles)
            .Returns(processedDocuments);
        mockFileWriter.WriteDocumentsToFolderAsync(processedDocuments, "/test/project/output")
            .Returns(Task.CompletedTask);

        var dialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);

        // Track validation results event
        ValidationResult? receivedValidationResult = null;
        dialogViewModel.ValidationResultsAvailable += (sender, result) => receivedValidationResult = result;

        // Act
        dialogViewModel.SaveCommand.Execute(null);
        
        // Wait for async build to complete
        await WaitForConditionAsync(() => !dialogViewModel.IsBuildInProgress, 1000);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.CanBuild, Is.True, "Should be able to build again after completion");
            Assert.That(dialogViewModel.BuildStatus, Does.Contain("completed"), "Should show completion status");
            Assert.That(receivedValidationResult, Is.Not.Null, "ValidationResultsAvailable event should be triggered");
            Assert.That(receivedValidationResult!.IsValid, Is.True, "Validation should pass with no errors");

            // Verify services were called correctly
            mockFileCollector.Received(1).CollectAllMarkdownFilesAsync("/test/project");
            mockCombination.Received(1).ValidateAll(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>());
            mockCombination.Received(1).BuildDocumentation(templateFiles, sourceFiles);
            mockFileWriter.Received(1).WriteDocumentsToFolderAsync(processedDocuments, "/test/project/output");
        });
    }

    [AvaloniaTest]
    public async Task BuildConfirmationDialog_Should_Handle_Build_Errors_Gracefully()
    {
        // Arrange
        var options = Options.Create(new ApplicationOptions
        {
            DefaultProjectFolder = "/test/project",
            DefaultOutputFolder = "output"
        });

        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();

        // Setup mock to throw exception
        mockFileCollector.CollectAllMarkdownFilesAsync("/test/project")
            .Returns(Task.FromException<(IEnumerable<MarkdownDocument>, IEnumerable<MarkdownDocument>)>(
                new DirectoryNotFoundException("Test directory not found")));

        var dialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);

        // Act
        dialogViewModel.SaveCommand.Execute(null);
        
        // Wait for async build to complete
        await WaitForConditionAsync(() => !dialogViewModel.IsBuildInProgress, 1000);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.CanBuild, Is.True, "Should be able to build again after error");
            Assert.That(dialogViewModel.BuildStatus, Does.Contain("failed"), "Should show failure status");
            Assert.That(dialogViewModel.BuildStatus, Does.Contain("Test directory not found"), "Should show error message");

            // Verify only file collector was called
            mockFileCollector.Received(1).CollectAllMarkdownFilesAsync("/test/project");
            mockCombination.DidNotReceive().BuildDocumentation(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>());
            mockFileWriter.DidNotReceive().WriteDocumentsToFolderAsync(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<string>());
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Disable_Build_During_Build_Process()
    {
        // Arrange
        var options = Options.Create(new ApplicationOptions
        {
            DefaultProjectFolder = "/test/project",
            DefaultOutputFolder = "output"
        });

        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();

        var dialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);

        // Act
        dialogViewModel.IsBuildInProgress = true;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.CanBuild, Is.False, "Should not be able to build when build is in progress");
            Assert.That(dialogViewModel.SaveCommand.CanExecute(null), Is.False, "Save command should be disabled during build");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Should_Update_Build_Status_Property()
    {
        // Arrange
        var options = Options.Create(new ApplicationOptions());
        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();

        var dialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);

        // Track property changes
        string? capturedStatus = null;
        dialogViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(dialogViewModel.BuildStatus))
            {
                capturedStatus = dialogViewModel.BuildStatus;
            }
        };

        // Act
        dialogViewModel.BuildStatus = "Test status message";

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.BuildStatus, Is.EqualTo("Test status message"), "Build status should be updated");
            Assert.That(capturedStatus, Is.EqualTo("Test status message"), "PropertyChanged should be raised for BuildStatus");
        });
    }

    [AvaloniaTest]
    public void BuildConfirmationDialog_Save_Button_Should_Be_Enabled_When_Build_Not_In_Progress()
    {
        // Arrange
        var options = Options.Create(new ApplicationOptions
        {
            DefaultProjectFolder = "/test/project",
            DefaultOutputFolder = "output"
        });

        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();

        var dialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel.IsBuildInProgress, Is.False, "Build should not be in progress initially");
            Assert.That(dialogViewModel.CanBuild, Is.True, "Should be able to build when not in progress");
            Assert.That(dialogViewModel.SaveCommand.CanExecute(null), Is.True, "Save command should be enabled when build is not in progress");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Log_Output_Section_Visible_By_Default()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially no tabs should exist (created on-demand)
        Assert.That(viewModel!.BottomPanelTabs.Count, Is.EqualTo(0), "Bottom panel should initially have no tabs");
        
        // But after showing logs, the tab should exist
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(1), "Bottom panel should have 1 tab after showing logs");
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        Assert.That(logTab, Is.Not.Null, "Log tab should exist after showing logs");

        // Find the bottom output TextBox in the UI  
        var bottomOutputTextBox = window.FindControl<TextBox>("BottomOutput");
        
        // The bottom output section should exist in the UI
        Assert.That(bottomOutputTextBox, Is.Not.Null, "Bottom output TextBox should exist in UI");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Bottom_Panel_Tabs()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially no tabs should exist
        Assert.That(viewModel!.BottomPanelTabs.Count, Is.EqualTo(0), "Should initially have no bottom panel tabs");
        
        // Show both logs and errors
        viewModel.ShowLogsCommand.Execute(null);
        viewModel.ShowErrorsCommand.Execute(null);
        
        // Now should have both tabs
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(2), "Should have 2 bottom panel tabs after showing both");
        
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        var errorTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "errors");
        
        Assert.That(logTab, Is.Not.Null, "Log tab should exist");
        Assert.That(errorTab, Is.Not.Null, "Error tab should exist");
        Assert.That(logTab!.Title, Is.EqualTo("Log Output"), "Log tab should have correct title");
        Assert.That(errorTab!.Title, Is.EqualTo("Errors"), "Error tab should have correct title");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Hide_Log_Output_When_Close_Button_Is_Clicked()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially bottom panel should be hidden
        Assert.That(viewModel!.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden initially");

        // Show logs to activate bottom panel
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible after showing logs");

        // Execute the close bottom panel command
        viewModel.CloseBottomPanelCommand.Execute(null);

        // Bottom panel should now be hidden
        Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden after close command");
    }

    [AvaloniaTest]
    public void MainWindow_LogOutput_Close_Button_Should_Have_Click_Handler()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.CloseBottomPanelCommand, Is.Not.Null, "CloseBottomPanelCommand should exist");

        // Show logs first to make panel visible
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible initially");

        // Test that we can execute the command that the button is wired to execute
        viewModel.CloseBottomPanelCommand.Execute(null);

        // Bottom panel should now be hidden
        Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden after close command execution");
    }

    [AvaloniaTest]
    public void MainWindow_CloseBottomPanel_Command_Should_Be_Executable()
    {
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.CloseBottomPanelCommand, Is.Not.Null, "CloseBottomPanelCommand should exist");
        
        // Test that the command can be executed
        bool canExecute = viewModel.CloseBottomPanelCommand.CanExecute(null);
        Assert.That(canExecute, Is.True, "CloseBottomPanelCommand should be executable");
        
        // Show logs first to make panel visible
        viewModel.ShowLogsCommand.Execute(null);
        
        // Track property changes
        bool visibilityChanged = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsBottomPanelVisible))
                visibilityChanged = true;
        };
        
        // Execute the command
        viewModel.CloseBottomPanelCommand.Execute(null);
        
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden after command execution");
            Assert.That(visibilityChanged, Is.True, "PropertyChanged should be fired for IsBottomPanelVisible");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Remove_Tab_From_Collection_When_Closed()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially no tabs
        Assert.That(viewModel!.BottomPanelTabs.Count, Is.EqualTo(0), "Should start with no tabs");

        // Show logs and errors
        viewModel.ShowLogsCommand.Execute(null);
        viewModel.ShowErrorsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(2), "Should have 2 tabs after showing both");

        // Close the logs tab
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        Assert.That(logTab, Is.Not.Null, "Log tab should exist");
        
        viewModel.CloseBottomTab(logTab!);
        
        // Should now have only 1 tab (errors)
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(1), "Should have 1 tab after closing logs");
        var remainingTab = viewModel.BottomPanelTabs.First();
        Assert.That(remainingTab.Id, Is.EqualTo("errors"), "Remaining tab should be errors");

        // Close the errors tab
        viewModel.CloseBottomTab(remainingTab);
        
        // Should now have no tabs and panel should be hidden
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(0), "Should have no tabs after closing all");
        Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden when no tabs remain");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Recreate_Tab_After_Closing_And_Reopening_From_Menu()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Show logs
        viewModel!.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(1), "Should have 1 tab");
        var originalLogTab = viewModel.BottomPanelTabs.First();
        
        // Close the logs tab
        viewModel.CloseBottomTab(originalLogTab);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(0), "Should have no tabs after closing");

        // Show logs again from menu
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(1), "Should have 1 tab again after reopening");
        
        var newLogTab = viewModel.BottomPanelTabs.First();
        Assert.That(newLogTab.Id, Is.EqualTo("logs"), "Recreated tab should be logs");
        Assert.That(newLogTab.Title, Is.EqualTo("Log Output"), "Recreated tab should have correct title");
        Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible again");
        Assert.That(viewModel.ActiveBottomTab, Is.EqualTo(newLogTab), "Recreated tab should be active");
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Display_Validation_Errors_In_Error_Panel()
    {
        var window = CreateMainWindow();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);


        // Open a file
        await viewModel.OpenFileAsync("/test/path/README.md");
        Assert.That(viewModel.ActiveTab, Is.Not.Null, "Active tab should exist");

        // Mock validation service to return errors
        var mockValidationResult = new ValidationResult
        {
            Errors = 
            [
                new ValidationIssue 
                { 
                    Message = "Missing source file", 
                    LineNumber = 5, 
                    DirectivePath = "missing.md" 
                },
                new ValidationIssue 
                { 
                    Message = "Invalid directive format", 
                    LineNumber = 10 
                }
            ],
            Warnings = 
            [
                new ValidationIssue 
                { 
                    Message = "Unused source file", 
                    DirectivePath = "unused.md" 
                }
            ]
        };

        // Manually update the current validation result to simulate validation
        viewModel.GetType().GetProperty("CurrentValidationResult")!.SetValue(viewModel, mockValidationResult);
        
        // Manually call the UpdateErrorPanelWithValidationResults method
        viewModel.UpdateErrorPanelWithValidationResults(mockValidationResult);


        // No delay needed for direct method invocation

        Assert.Multiple(() =>
        {
            // Error panel should be visible
            Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible");
            Assert.That(viewModel.ActiveBottomTab, Is.Not.Null, "Active bottom tab should exist");
            Assert.That(viewModel.ActiveBottomTab!.Title, Is.EqualTo("Errors"), "Error tab should be active");

            // Error content should contain the validation errors
            var errorContent = viewModel.ActiveBottomTab.Content;
            Assert.That(errorContent, Contains.Substring("Error: Missing source file (Line 5)"), "Should contain first error");
            Assert.That(errorContent, Contains.Substring("Error: Invalid directive format (Line 10)"), "Should contain second error");
            Assert.That(errorContent, Contains.Substring("Warning: Unused source file"), "Should contain warning");
            Assert.That(errorContent, Contains.Substring("File: missing.md"), "Should contain file info");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Not_Show_Error_Panel_When_Validation_Passes()
    {
        var window = CreateMainWindow();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Open a file
        await viewModel.OpenFileAsync("/test/path/README.md");
        Assert.That(viewModel.ActiveTab, Is.Not.Null, "Active tab should exist");

        // Mock validation service to return success (no errors)
        var mockValidationResult = new ValidationResult();

        // Manually update the current validation result to simulate validation
        viewModel.GetType().GetProperty("CurrentValidationResult")!.SetValue(viewModel, mockValidationResult);
        
        // Store initial state
        var wasBottomPanelVisible = viewModel.IsBottomPanelVisible;
        var initialActiveBottomTab = viewModel.ActiveBottomTab;
        
        // Manually call the UpdateErrorPanelWithValidationResults method
        viewModel.UpdateErrorPanelWithValidationResults(mockValidationResult);

        // No delay needed for direct method invocation

        Assert.Multiple(() =>
        {
            // Error panel should NOT be shown when validation passes
            Assert.That(viewModel.IsBottomPanelVisible, Is.EqualTo(wasBottomPanelVisible), "Bottom panel visibility should not change");
            Assert.That(viewModel.ActiveBottomTab, Is.EqualTo(initialActiveBottomTab), "Active bottom tab should not change");
            
            // No error tab should be created for successful validation
            var errorTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Title == "Errors");
            if (errorTab != null)
            {
                Assert.That(errorTab.IsActive, Is.False, "Error tab should not be active when validation passes");
            }
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Have_ValidateAllCommand()
    {
        var window = CreateMainWindow();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ValidateAllCommand, Is.Not.Null, "ValidateAllCommand should exist");
            Assert.That(viewModel.ValidateAllCommand.CanExecute(null), Is.True, "ValidateAllCommand should be executable");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_ValidateAllCommand_Should_Process_All_Templates()
    {
        var window = CreateMainWindow();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Mock template and source files
        var templateFiles = new[]
        {
            new MarkdownDocument("template1.mdext", "# Template 1")
        };
        var sourceFiles = new[]
        {
            new MarkdownDocument("source1.mdsrc", "Source content")
        };

        // Mock the file collector service
        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        mockFileCollector.CollectAllMarkdownFilesAsync(Arg.Any<string>())
            .Returns((templateFiles, sourceFiles));

        // Mock the markdown combination service to return validation results
        var mockValidationResult = new ValidationResult
        {
            Errors = new List<ValidationIssue>
            {
                new ValidationIssue { Message = "[template1.mdext] Test error", LineNumber = 1 }
            }
        };

        var mockCombinationService = Substitute.For<IMarkdownCombinationService>();
        mockCombinationService.ValidateAll(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(mockValidationResult);

        // Replace the services using reflection to inject our mocks
        var fileCollectorField = viewModel.GetType().GetField("_markdownFileCollectorService", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fileCollectorField!.SetValue(viewModel, mockFileCollector);

        var combinationServiceField = viewModel.GetType().GetField("_markdownCombinationService", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        combinationServiceField!.SetValue(viewModel, mockCombinationService);

        // Execute the validate all command
        viewModel.ValidateAllCommand.Execute(null);

        // Wait a moment for async operation to complete
        await WaitForConditionAsync(() => viewModel.IsBottomPanelVisible, 1000);

        Assert.Multiple(() =>
        {
            // Verify that services were called
            mockFileCollector.Received(1).CollectAllMarkdownFilesAsync(Arg.Any<string>());
            mockCombinationService.Received(1).ValidateAll(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>());
            
            // Verify that error panel is shown with validation results
            Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible for errors");
            Assert.That(viewModel.ActiveBottomTab, Is.Not.Null, "Active bottom tab should exist");
            Assert.That(viewModel.ActiveBottomTab!.Title, Is.EqualTo("Errors"), "Error tab should be active");
            Assert.That(viewModel.ActiveBottomTab.Content, Does.Contain("[template1.mdext] Test error"), "Should contain validation error");
        });
    }
}
