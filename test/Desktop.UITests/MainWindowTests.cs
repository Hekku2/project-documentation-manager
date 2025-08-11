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

namespace Desktop.UITests;

public class MainWindowTests
{
    private class MockFileService : IFileService
    {
        public Task<FileSystemItem?> GetFileStructureAsync()
        {
            return Task.FromResult<FileSystemItem?>(new FileSystemItem
            {
                Name = "test-project",
                FullPath = "/test/path",
                IsDirectory = true,
                Children = new List<FileSystemItem>
                {
                    new FileSystemItem { Name = "src", IsDirectory = true },
                    new FileSystemItem { Name = "test", IsDirectory = true },
                    new FileSystemItem { Name = "README.md", IsDirectory = false }
                }
            });
        }

        public Task<FileSystemItem?> GetFileStructureAsync(string folderPath)
        {
            return GetFileStructureAsync();
        }

        public bool IsValidFolder(string folderPath)
        {
            return true;
        }
    }

    private MainWindow CreateMainWindow()
    {
        var vmLogger = new LoggerFactory().CreateLogger<MainWindowViewModel>();
        var options = Options.Create(new ApplicationOptions());
        var fileService = new MockFileService();
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
        Assert.That(documentEditor, Is.Not.Null, "Document editor not found");
        Assert.That(documentEditor!.AcceptsReturn, Is.True, "Editor should accept return");
        Assert.That(documentEditor.AcceptsTab, Is.True, "Editor should accept tab");
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
}
