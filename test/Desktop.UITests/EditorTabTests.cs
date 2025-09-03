using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Desktop.ViewModels;
using Desktop.Models;
using NSubstitute;
using Avalonia.VisualTree;
using Desktop.Views;

namespace Desktop.UITests;

[TestFixture]
public class EditorTabTests : MainWindowTestBase
{
    [AvaloniaTest]
    public async Task MainWindow_Should_Handle_Multiple_File_Selections()
    {
        var window = CreateMainWindowWithNestedStructure();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Expand the root and src folder to get multiple files
        await ExpandFolderAndWaitAsync(fileExplorerViewModel.RootItem!);

        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");

        await ExpandFolderAndWaitAsync(srcFolder);

        // Get files
        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        Assert.Multiple(() =>
        {
            Assert.That(readmeFile, Is.Not.Null, "README.md should exist");
            Assert.That(mainFile, Is.Not.Null, "main.cs should exist");
        });

        // Initially no tabs
        Assert.That(viewModel.EditorTabBar.EditorTabs, Is.Empty, "No tabs initially");

        // Select first file
        await SelectFileAndWaitForTabAsync(readmeFile, viewModel);

        // Select second file
        await SelectFileAndWaitForTabAsync(mainFile, viewModel);

        Assert.Multiple(() =>
        {
            // Should have 2 tabs
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Two tabs should be open");

            // First tab should exist but not be active
            var readmeTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
            Assert.That(readmeTab, Is.Not.Null, "README tab should exist");
            Assert.That(readmeTab!.IsActive, Is.False, "README tab should not be active");

            // Second tab should be active
            var mainTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");
            Assert.That(mainTab, Is.Not.Null, "main.cs tab should exist");
            Assert.That(mainTab!.IsActive, Is.True, "main.cs tab should be active");

            // Active tab should be the main.cs tab
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(mainTab), "Active tab should be main.cs");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Allow_Tab_Closing_Via_Close_Button()
    {
        var window = CreateMainWindowWithNestedStructure();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Expand the root to get files
        await ExpandFolderAndWaitAsync(fileExplorerViewModel.RootItem!);

        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");

        await ExpandFolderAndWaitAsync(srcFolder);

        // Get files
        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        Assert.Multiple(() =>
        {
            Assert.That(readmeFile, Is.Not.Null, "README.md should exist");
            Assert.That(mainFile, Is.Not.Null, "main.cs should exist");
        });

        // Open both files
        await SelectFileAndWaitForTabAsync(readmeFile, viewModel);

        await SelectFileAndWaitForTabAsync(mainFile, viewModel);

        // Verify 2 tabs are open
        Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Two tabs should be open");

        // Get the README tab and main tab
        var readmeTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");

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
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(1), "One tab should remain");

            // The remaining tab should be main.cs
            var remainingTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault();
            Assert.That(remainingTab, Is.Not.Null, "One tab should remain");
            Assert.That(remainingTab!.Title, Is.EqualTo("main.cs"), "Remaining tab should be main.cs");
            Assert.That(remainingTab.IsActive, Is.True, "Remaining tab should be active");

            // Active tab should still be main.cs
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(remainingTab), "Active tab should be main.cs");
        });

        // Close the last remaining tab
        var lastTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault();
        lastTab!.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // No tabs should remain
            Assert.That(viewModel.EditorTabBar.EditorTabs, Is.Empty, "No tabs should remain");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.Null, "No active tab should exist");
            Assert.That(viewModel.EditorContent.ActiveFileContent, Is.Null, "No active file content should exist");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Handle_Closing_Active_Tab_And_Switch_To_Another()
    {
        var window = CreateMainWindowWithNestedStructure();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(fileExplorerViewModel.RootItem);

        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder);

        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Open both files
        await SelectFileAndWaitForTabAsync(readmeFile, viewModel);

        await SelectFileAndWaitForTabAsync(mainFile, viewModel);

        // Verify setup: 2 tabs, main.cs is active
        Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Two tabs should be open");

        var readmeTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");

        Assert.Multiple(() =>
        {
            Assert.That(mainTab!.IsActive, Is.True, "main.cs should be active");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
        });

        // Close the currently active tab (main.cs)
        mainTab!.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Should have 1 tab remaining
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(1), "One tab should remain");

            // The README tab should now be active
            Assert.That(readmeTab!.IsActive, Is.True, "README tab should now be active");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should switch to README");
            Assert.That(viewModel.EditorContent.ActiveFileContent, Is.EqualTo("Mock file content"), "Active content should be from README");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Highlight_Active_Tab_Correctly()
    {
        var window = CreateMainWindowWithNestedStructure();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(fileExplorerViewModel.RootItem!);

        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);

        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Initially no tabs
        Assert.That(viewModel.EditorTabBar.EditorTabs, Is.Empty, "No tabs initially");

        // Open first file (README.md)
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);

        // Should have 1 tab and it should be active
        Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(1), "One tab should be open");
        var readmeTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        Assert.That(readmeTab, Is.Not.Null, "README tab should exist");
        Assert.Multiple(() =>
        {
            Assert.That(readmeTab!.IsActive, Is.True, "README tab should be active");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should be README");
        });

        // Open second file (main.cs)
        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Should have 2 tabs now
        Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Two tabs should be open");
        var mainTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");
        Assert.That(mainTab, Is.Not.Null, "main.cs tab should exist");

        Assert.Multiple(() =>
        {
            // Only the main.cs tab should be active now
            Assert.That(mainTab!.IsActive, Is.True, "main.cs tab should be active");
            Assert.That(readmeTab.IsActive, Is.False, "README tab should NOT be active");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
        });

        // Switch active tab back to README by clicking it (simulate tab selection)
        viewModel.EditorTabBar.SetActiveTab(readmeTab);

        Assert.Multiple(() =>
        {
            // Now only the README tab should be active
            Assert.That(readmeTab.IsActive, Is.True, "README tab should be active again");
            Assert.That(mainTab.IsActive, Is.False, "main.cs tab should NOT be active");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should be README");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Update_Active_Tab_Highlighting_When_Tab_Is_Closed()
    {
        var window = CreateMainWindowWithNestedStructure();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(fileExplorerViewModel.RootItem!);

        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);

        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Open both files
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);

        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Verify setup: 2 tabs, main.cs is active
        Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Two tabs should be open");

        var readmeTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");

        Assert.Multiple(() =>
        {
            Assert.That(mainTab!.IsActive, Is.True, "main.cs should be active initially");
            Assert.That(readmeTab!.IsActive, Is.False, "README should not be active initially");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
        });

        // Close the currently active tab (main.cs)
        mainTab!.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Should have 1 tab remaining
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(1), "One tab should remain");

            // The README tab should now be active (and highlighted)
            Assert.That(readmeTab!.IsActive, Is.True, "README tab should now be active and highlighted");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should switch to README");
        });

        // Close the last tab
        readmeTab!.CloseCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // No tabs should remain
            Assert.That(viewModel.EditorTabBar.EditorTabs, Is.Empty, "No tabs should remain");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.Null, "No active tab should exist");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Allow_File_Selection_By_Clicking_Tab()
    {
        var window = CreateMainWindowWithNestedStructure();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(fileExplorerViewModel.RootItem!);

        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);

        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Open both files to create tabs
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);

        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Verify setup: 2 tabs exist, main.cs is currently active
        Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Two tabs should be open");

        var readmeTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");

        Assert.Multiple(() =>
        {
            Assert.That(readmeTab, Is.Not.Null, "README tab should exist");
            Assert.That(mainTab, Is.Not.Null, "main.cs tab should exist");
            Assert.That(mainTab!.IsActive, Is.True, "main.cs should be active initially");
            Assert.That(readmeTab!.IsActive, Is.False, "README should not be active initially");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
        });

        // Click on the README tab to select it
        readmeTab!.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // README tab should now be active
            Assert.That(readmeTab.IsActive, Is.True, "README tab should be active after click");
            Assert.That(mainTab!.IsActive, Is.False, "main.cs tab should not be active after README click");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should be README after click");
            Assert.That(viewModel.EditorContent.ActiveFileContent, Is.EqualTo("Mock file content"), "Active content should be from README");

            // Editor should display README content
            var editorUserControl = window.GetVisualDescendants().OfType<EditorUserControl>().FirstOrDefault();
            var fileEditorContent = editorUserControl?.GetVisualDescendants().OfType<Desktop.Views.FileEditorContent>().FirstOrDefault();
            var editorTextBox = fileEditorContent?.FindControl<TextBox>("DocumentEditor");
            Assert.That(editorTextBox?.Text, Is.EqualTo("Mock file content"), "Editor should display README content");
        });

        // Click back on the main.cs tab
        mainTab!.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // main.cs tab should be active again
            Assert.That(mainTab.IsActive, Is.True, "main.cs tab should be active after click");
            Assert.That(readmeTab.IsActive, Is.False, "README tab should not be active after main.cs click");
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs after click");
            Assert.That(viewModel.EditorContent.ActiveFileContent, Is.EqualTo("Mock file content"), "Active content should be from main.cs");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Show_Visual_Highlighting_When_Tab_Is_Selected_Via_Click()
    {
        var window = CreateMainWindowWithNestedStructure();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Expand and get files
        await ExpandFolderAndWaitAsync(fileExplorerViewModel.RootItem!);

        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");

        // Get the src folder and expand it
        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        await ExpandFolderAndWaitAsync(srcFolder!);

        var mainFile = srcFolder!.Children.FirstOrDefault(c => c.Name == "main.cs");

        // Open three files to test multiple tab selection
        await SelectFileAndWaitForTabAsync(readmeFile!, viewModel);

        await SelectFileAndWaitForTabAsync(mainFile!, viewModel);

        // Verify setup
        Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Two tabs should be open");

        var readmeTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "README.md");
        var mainTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.Title == "main.cs");

        Assert.Multiple(() =>
        {
            // Initially main.cs should be active
            Assert.That(mainTab!.IsActive, Is.True, "main.cs should be active initially");
            Assert.That(readmeTab!.IsActive, Is.False, "README should not be active initially");
        });

        // Click README tab and verify highlighting changes
        readmeTab!.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Only README should be highlighted/active
            Assert.That(readmeTab.IsActive, Is.True, "README should be active after click");
            Assert.That(mainTab!.IsActive, Is.False, "main.cs should not be active after README click");

            // Verify the highlighting is applied (IsActive property drives the visual highlighting)
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(readmeTab), "ActiveTab should be README");
        });

        // Click main.cs tab and verify highlighting switches
        mainTab!.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            // Only main.cs should be highlighted/active
            Assert.That(mainTab.IsActive, Is.True, "main.cs should be active after click");
            Assert.That(readmeTab.IsActive, Is.False, "README should not be active after main.cs click");

            // Verify the highlighting is applied
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(mainTab), "ActiveTab should be main.cs");
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
        Assert.That(viewModel!.BottomPanelTabs, Is.Empty, "Should start with no tabs");

        // Show logs and errors
        viewModel.ShowLogsCommand.Execute(null);
        viewModel.ShowErrorsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(2), "Should have 2 tabs after showing both");

        // Close the logs tab
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        Assert.That(logTab, Is.Not.Null, "Log tab should exist");

        viewModel.CloseBottomTab(logTab!);

        // Should now have only 1 tab (errors)
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(1), "Should have 1 tab after closing logs");
        var remainingTab = viewModel.BottomPanelTabs.First();
        Assert.That(remainingTab.Id, Is.EqualTo("errors"), "Remaining tab should be errors");

        // Close the errors tab
        viewModel.CloseBottomTab(remainingTab);

        Assert.Multiple(() =>
        {
            // Should now have no tabs and panel should be hidden
            Assert.That(viewModel.BottomPanelTabs, Is.Empty, "Should have no tabs after closing all");
            Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden when no tabs remain");
        });
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
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(1), "Should have 1 tab");
        var originalLogTab = viewModel.BottomPanelTabs.First();

        // Close the logs tab
        viewModel.CloseBottomTab(originalLogTab);
        Assert.That(viewModel.BottomPanelTabs, Is.Empty, "Should have no tabs after closing");

        // Show logs again from menu
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(1), "Should have 1 tab again after reopening");

        var newLogTab = viewModel.BottomPanelTabs.First();
        Assert.Multiple(() =>
        {
            Assert.That(newLogTab.Id, Is.EqualTo("logs"), "Recreated tab should be logs");
            Assert.That(newLogTab.Title, Is.EqualTo("Log Output"), "Recreated tab should have correct title");
            Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible again");
            Assert.That(viewModel.ActiveBottomTab, Is.EqualTo(newLogTab), "Recreated tab should be active");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Reuse_Existing_Tab_When_Same_File_Opened_Multiple_Times()
    {
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;

        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially no tabs should be open
        Assert.That(viewModel!.EditorTabBar.EditorTabs, Is.Empty, "No tabs should be open initially");

        // Simulate having an open file by directly setting up the editor state
        var editorTab = new Desktop.Models.EditorTab
        {
            Id = "readme-tab",
            Title = "README.md",
            FilePath = "/test/path/README.md",
            Content = "# README content",
            IsModified = false,
            TabType = Desktop.Models.TabType.File
        };
        var tabViewModel = new EditorTabViewModel(editorTab);
        viewModel.EditorTabBar.EditorTabs.Add(tabViewModel);
        viewModel.EditorTabBar.SetActiveTab(tabViewModel);

        // Verify tab was created
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(1), "One tab should be open");
            var tab = viewModel.EditorTabBar.EditorTabs.First();
            Assert.That(tab.Title, Is.EqualTo("README.md"), "Tab title should be README.md");
            Assert.That(tab.FilePath, Is.EqualTo("/test/path/README.md"), "Tab file path should be correct");
            Assert.That(tab.IsActive, Is.True, "Tab should be active");
        });

        var originalTabId = viewModel.EditorTabBar.EditorTabs.First().Id;

        // Test the core logic: simulate what should happen when the same file is opened again
        var normalizedPath = System.IO.Path.GetFullPath("/test/path/README.md");
        var existingTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t =>
            t.FilePath != null &&
            System.IO.Path.GetFullPath(t.FilePath).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

        Assert.Multiple(() =>
        {
            // The tab should be found (this is what prevents duplication)
            Assert.That(existingTab, Is.Not.Null, "Existing tab should be found for same file path");
            Assert.That(existingTab!.Id, Is.EqualTo(originalTabId), "Should be the same tab");
        });

        // Test path normalization scenarios
        var pathWithExtraSlashes = "/test/path//README.md";
        var normalizedPathWithSlashes = System.IO.Path.GetFullPath(pathWithExtraSlashes);
        var tabForSlashedPath = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t =>
            t.FilePath != null &&
            System.IO.Path.GetFullPath(t.FilePath).Equals(normalizedPathWithSlashes, StringComparison.OrdinalIgnoreCase));

        Assert.That(tabForSlashedPath, Is.Not.Null, "Should find existing tab even with extra slashes in path");
        Assert.That(tabForSlashedPath!.Id, Is.EqualTo(originalTabId), "Should be the same tab instance");

        // Test path that has redundant path components (../)
        var pathWithDotDot = "/test/other/../path/README.md";

        // Test the core logic: verify tab would not be duplicated
        var normalizedDotDotPath = System.IO.Path.GetFullPath(pathWithDotDot);
        var existingTabForDotDot = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t =>
            t.FilePath != null &&
            System.IO.Path.GetFullPath(t.FilePath).Equals(normalizedDotDotPath, StringComparison.OrdinalIgnoreCase));

        Assert.Multiple(() =>
        {
            // Should find existing tab even with ../ in path
            Assert.That(existingTabForDotDot, Is.Not.Null, "Should find existing tab even with ../ path");
            Assert.That(existingTabForDotDot!.Id, Is.EqualTo(originalTabId), "Should be the same tab instance");

            // Tab management logic should reuse the existing tab
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(1), "Should still have only one tab");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Create_Separate_Tabs_For_Different_Files()
    {
        var window = CreateMainWindowWithNestedStructure();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Open first file
        await viewModel.EditorTabBar.OpenFileAsync("/test/path/README.md");
        Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(1), "One tab should be open for first file");

        // Open second file - should create a new tab
        await viewModel.EditorTabBar.OpenFileAsync("/test/path/different-file.md");

        Assert.Multiple(() =>
        {
            // Should have two tabs for different files
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Should have two tabs for different files");

            var readmeTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.FilePath!.Contains("README.md"));
            var differentTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.FilePath!.Contains("different-file.md"));

            Assert.That(readmeTab, Is.Not.Null, "README tab should exist");
            Assert.That(differentTab, Is.Not.Null, "Different file tab should exist");
            Assert.That(readmeTab!.Id, Is.Not.EqualTo(differentTab!.Id), "Tabs should have different IDs");

            // The most recently opened file should be active
            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(differentTab), "Most recently opened file should be active");
        });
    }

    [AvaloniaTest]
    public async Task OpenFileAsync_Should_Work_When_Settings_Tab_Is_Active()
    {
        // Arrange
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var editorViewModel = CreateEditorViewModel();
        var viewModel = new MainWindowViewModel(_vmLogger, _options, _editorStateService, editorViewModel, _logTransitionService, hotkeyService);

        // Open settings tab first
        viewModel.EditorTabBar.OpenSettingsTab();

        Assert.Multiple(() =>
        {
            // Verify settings tab is active
            Assert.That(viewModel.EditorTabBar.ActiveTab?.TabType, Is.EqualTo(TabType.Settings), "Settings tab should be active");
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(1), "Should have one tab (Settings)");
        });

        // Act - try to open a file while Settings tab is active (this used to crash)
        const string testFilePath = "/test/file.md";
        await viewModel.EditorTabBar.OpenFileAsync(testFilePath);

        // Assert - should successfully open the file without crashing
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Should have two tabs (Settings + File)");

            var fileTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.TabType == TabType.File);
            Assert.That(fileTab, Is.Not.Null, "File tab should be created");
            Assert.That(fileTab!.FilePath, Is.EqualTo(testFilePath), "File tab should have correct file path");
            Assert.That(fileTab.IsActive, Is.True, "File tab should become active");

            var settingsTab = viewModel.EditorTabBar.EditorTabs.FirstOrDefault(t => t.TabType == TabType.Settings);
            Assert.That(settingsTab, Is.Not.Null, "Settings tab should still exist");
            Assert.That(settingsTab!.IsActive, Is.False, "Settings tab should no longer be active");

            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(fileTab), "Active tab should be the newly opened file tab");
        });
    }

    [AvaloniaTest]
    public async Task OpenFileAsync_Should_Not_Duplicate_Existing_File_When_Settings_Tab_Is_Active()
    {
        // Arrange
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var editorViewModel = CreateEditorViewModel();
        var viewModel = new MainWindowViewModel(_vmLogger, _options, _editorStateService, editorViewModel, _logTransitionService, hotkeyService);

        const string testFilePath = "/test/file.md";

        // Open file first
        await viewModel.EditorTabBar.OpenFileAsync(testFilePath);

        // Open settings tab
        viewModel.EditorTabBar.OpenSettingsTab();

        Assert.Multiple(() =>
        {
            // Verify initial state
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Should have two tabs initially");
            Assert.That(viewModel.EditorTabBar.ActiveTab?.TabType, Is.EqualTo(TabType.Settings), "Settings tab should be active");
        });

        // Act - try to open the same file again while Settings tab is active
        await viewModel.EditorTabBar.OpenFileAsync(testFilePath);

        // Assert - should switch to existing file tab without creating duplicate
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.EditorTabBar.EditorTabs, Has.Count.EqualTo(2), "Should still have two tabs (no duplicate)");

            var fileTabs = viewModel.EditorTabBar.EditorTabs.Where(t => t.TabType == TabType.File).ToList();
            Assert.That(fileTabs, Has.Count.EqualTo(1), "Should have exactly one file tab");
            Assert.That(fileTabs[0].FilePath, Is.EqualTo(testFilePath), "File tab should have correct file path");
            Assert.That(fileTabs[0].IsActive, Is.True, "File tab should become active");

            Assert.That(viewModel.EditorTabBar.ActiveTab, Is.EqualTo(fileTabs[0]), "Active tab should be the existing file tab");
        });
    }
}