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

namespace Desktop.UITests;

public class MainWindowTests
{
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
        
        fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        
        var viewModel = new MainWindowViewModel(vmLogger, options, fileService);
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
    public void MainWindow_Should_Have_Menu_Bar()
    {
        var window = CreateMainWindow();
        window.Show();

        var menu = window.FindControl<Menu>("MainMenu");
        Assert.That(menu, Is.Not.Null, "Main menu not found");
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
    public void MainWindow_Should_Have_Welcome_Tab()
    {
        var window = CreateMainWindow();
        window.Show();

        var welcomeTab = window.FindControl<Button>("WelcomeTab");
        Assert.That(welcomeTab, Is.Not.Null, "Welcome tab not found");
        Assert.That(welcomeTab!.Content, Is.EqualTo("Welcome"));
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
        
        fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateNestedTestStructure()));
        fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateNestedTestStructure()));
        fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        
        var viewModel = new MainWindowViewModel(vmLogger, options, fileService);
        return new MainWindow(viewModel);
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Only_Expand_Root_Level_By_Default()
    {
        var window = CreateMainWindowWithNestedStructure();
        window.Show();

        // Wait a bit for the async loading to complete
        await Task.Delay(500);

        // Get the TreeView
        var treeView = window.GetVisualDescendants().OfType<TreeView>().FirstOrDefault();
        Assert.That(treeView, Is.Not.Null, "TreeView not found");

        // Get the ViewModel from the window's DataContext
        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "MainWindowViewModel not found");

        // Wait for file structure to load
        var maxWait = 50; // 5 seconds max
        var waitCount = 0;
        while (viewModel!.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");
        
        // Root should be expanded (auto-expanded)
        Assert.That(viewModel.RootItem!.IsExpanded, Is.True, "Root folder should be expanded by default");

        // Find child folders in the root
        var srcFolder = viewModel.RootItem.Children.FirstOrDefault(c => c.Name == "src");
        var testFolder = viewModel.RootItem.Children.FirstOrDefault(c => c.Name == "test");

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
        window.Show();

        // Wait for initial load
        await Task.Delay(500);

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null);

        // Wait for file structure to load
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel!.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Get the src folder (should not be expanded initially)
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
        Assert.That(srcFolder!.IsExpanded, Is.False, "src folder should not be expanded initially");

        // Initially, src folder should have a placeholder child (Loading...)
        Assert.That(srcFolder.Children.Count, Is.EqualTo(1), "src folder should have placeholder child");
        Assert.That(srcFolder.Children[0].Name, Is.EqualTo("Loading..."), "Should have loading placeholder");

        // Expand the src folder by setting IsExpanded to true
        srcFolder.IsExpanded = true;

        // Wait for lazy loading to complete
        await Task.Delay(1000);

        // After expansion, children should be loaded
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.IsExpanded, Is.True, "src folder should be expanded");
            Assert.That(srcFolder.Children.Count, Is.EqualTo(3), "src folder should have 3 actual children");
            
            // Verify the actual children are loaded
            var componentsFolder = srcFolder.Children.FirstOrDefault(c => c.Name == "components");
            var utilsFolder = srcFolder.Children.FirstOrDefault(c => c.Name == "utils");
            var mainFile = srcFolder.Children.FirstOrDefault(c => c.Name == "main.cs");

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
        window.Show();

        await Task.Delay(500);

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel?.RootItem, Is.Not.Null);

        // Wait for file structure to load
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel!.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        // Get both src and test folders
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        var testFolder = viewModel.RootItem.Children.FirstOrDefault(c => c.Name == "test");

        Assert.Multiple(() =>
        {
            Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
            Assert.That(testFolder, Is.Not.Null, "test folder should exist");
        });

        // Expand both folders
        srcFolder!.IsExpanded = true;
        testFolder!.IsExpanded = true;

        // Wait for lazy loading
        await Task.Delay(1000);

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
}
